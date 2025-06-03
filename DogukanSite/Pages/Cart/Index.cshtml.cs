using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // Session için
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Logging için eklendi

namespace DogukanSite.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        // IHttpContextAccessor'a burada doðrudan ihtiyacýmýz olmayabilir, PageModel'ýn kendi HttpContext'i var.
        // Ama kullanýyorsanýz kalabilir.
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(DogukanSiteContext context,
                          UserManager<ApplicationUser> userManager,
                          IHttpContextAccessor httpContextAccessor, // Eðer GetSessionId'de HttpContext'i buradan alýyorsanýz
                          ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor; // Eðer kullanýlýyorsa
            _logger = logger;
        }

        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? AppliedCouponCode { get; set; }
        public decimal Total { get; set; }
        public bool IsCartEmpty => !Items.Any();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? CouponResponseMessage { get; set; }
        public bool CouponAppliedSuccessfully { get; set; } = false;

        public class CartItemViewModel
        {
            public int Id { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "Ürün Adý Yok";
            public string? ProductImageUrl { get; set; }
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal TotalPrice => UnitPrice * Quantity;
            public int MaxQuantity { get; set; }
        }

        private string? GetCurrentUserId()
        {
            // Bu metod PageModel.User üzerinden çalýþtýðý için IHttpContextAccessor'a gerek duymaz.
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return _userManager.GetUserId(User);
            }
            return null;
        }

        // ÖNEMLÝ: Bu GetSessionId metodunu Order/CreateModel.cs içindekiyle ayný mantýkta ve robustlukta yapalým.
        private string GetSessionId(bool createIfNull = true)
        {
            var currentPath = HttpContext.Request.Path; // Loglama için
            _logger.LogInformation("CartModel.GetSessionId çaðrýldý. Request Path: {Path}, createIfNull: {CreateIfNull}", currentPath, createIfNull);

            if (HttpContext.Session == null)
            {
                _logger.LogError("CartModel.GetSessionId: HttpContext.Session IS NULL! Session middleware (app.UseSession()) Program.cs'te doðru yapýlandýrýlmamýþ veya çaðrýlmamýþ olabilir. Path: {Path}", currentPath);
                throw new InvalidOperationException("HttpContext.Session is null. Session middleware doðru yapýlandýrýlmamýþ olabilir.");
            }

            if (!HttpContext.Session.IsAvailable)
            {
                _logger.LogWarning("CartModel.GetSessionId: HttpContext.Session.IsAvailable == false. Session yüklenemedi veya kullanýlamýyor. Path: {Path}", currentPath);
                // OrderModel'da olduðu gibi burada da hata fýrlatmak daha doðru olacaktýr.
                // Eðer createIfNull false ise ve session yoksa boþ string dönmek yerine null dönmek daha anlamlý olabilir.
                if (!createIfNull) return string.Empty; // veya null, çaðýran yerin null kontrolüne göre
                throw new InvalidOperationException($"Session is not available for path {currentPath}. Ensure session is configured and enabled, and app.UseSession() is correctly placed in Program.cs.");
            }

            string? sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId) && createIfNull)
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
                _logger.LogInformation("CartModel.GetSessionId: Yeni SessionId üretildi ve set edildi: {SessionId}. Path: {Path}", sessionId, currentPath);
            }
            else if (string.IsNullOrEmpty(sessionId) && !createIfNull)
            {
                _logger.LogWarning("CartModel.GetSessionId: SessionId null/empty ve createIfNull false. Boþ string dönülüyor. Path: {Path}", currentPath);
                return string.Empty; // veya null
            }
            else
            {
                _logger.LogInformation("CartModel.GetSessionId: Mevcut SessionId alýndý: {SessionId}. Path: {Path}", sessionId, currentPath);
            }
            return sessionId ?? string.Empty; // Null ise boþ string dön (string.IsNullOrEmpty kontrolü nedeniyle)
        }


        private async Task LoadCartItemsAsync()
        {
            _logger.LogInformation("CartModel.LoadCartItemsAsync çaðrýldý.");
            string? userId = GetCurrentUserId();
            // Kullanýcý giriþ yapmýþsa bile, birleþtirme için anonim SessionId'ye ihtiyacýmýz olabilir.
            // Eðer kullanýcý giriþ yapmýþ ve session'da ID yoksa, yeni ID oluþturmamalýyýz, çünkü sepeti UserID'ye baðlý olmalý.
            // Bu nedenle, createIfNull parametresini userId'nin durumuna göre ayarlýyoruz.
            string sessionId = GetSessionId(createIfNull: string.IsNullOrEmpty(userId));
            _logger.LogInformation("LoadCartItemsAsync: UserId={UserId}, SessionId={SessionId}", userId ?? "ANONYMOUS", sessionId);

            string? tempStatusMessage = null;
            IQueryable<CartItem> query;

            if (!string.IsNullOrEmpty(userId)) // Kullanýcý giriþ yapmýþ
            {
                // Adým 1: Eðer session'da bir "SessionId" varsa (kullanýcý giriþ yapmadan önce anonim sepeti olabilir),
                // bu anonim sepetteki ürünleri kullanýcýnýn veritabanýndaki sepetine birleþtir.
                string currentSessionIdForMerge = HttpContext.Session.GetString("SessionId") ?? string.Empty; // Sadece oku, üretme.

                if (!string.IsNullOrEmpty(currentSessionIdForMerge))
                {
                    var sessionCartItems = await _context.CartItems
                        .Where(ci => ci.SessionId == currentSessionIdForMerge && ci.ApplicationUserId == null)
                        .Include(ci => ci.Product)
                        .ToListAsync();

                    if (sessionCartItems.Any())
                    {
                        _logger.LogInformation("Merging {Count} session cart items from SessionId {OldSessionId} to UserID {UserId}", sessionCartItems.Count, currentSessionIdForMerge, userId);
                        foreach (var sessionItem in sessionCartItems.ToList())
                        {
                            if (sessionItem.Product == null || sessionItem.Product.Stock <= 0)
                            {
                                _context.CartItems.Remove(sessionItem);
                                tempStatusMessage = (tempStatusMessage ?? "") + $"'{sessionItem.Product?.Name ?? "Bilinmeyen bir ürün"}' stokta kalmadýðý için sepetinizden çýkarýldý. ";
                                continue;
                            }

                            var userCartItem = await _context.CartItems
                                .FirstOrDefaultAsync(ci => ci.ApplicationUserId == userId && ci.ProductId == sessionItem.ProductId);

                            if (userCartItem != null) // Kullanýcýnýn sepetinde bu ürün zaten var, adetleri birleþtir
                            {
                                int newQuantityForUserItem = userCartItem.Quantity + sessionItem.Quantity;
                                if (newQuantityForUserItem > sessionItem.Product.Stock)
                                {
                                    newQuantityForUserItem = sessionItem.Product.Stock;
                                    tempStatusMessage = (tempStatusMessage ?? "") + $"'{sessionItem.Product.Name}' ürünü için sepetinizdeki toplam adet stokla ({sessionItem.Product.Stock}) sýnýrlandýrýldý. ";
                                }
                                if (newQuantityForUserItem > 0) userCartItem.Quantity = newQuantityForUserItem;
                                else _context.CartItems.Remove(userCartItem);
                                _context.CartItems.Remove(sessionItem); // Anonim olaný sil
                            }
                            else // Kullanýcýnýn sepetinde bu ürün yok, anonim ürünü kullanýcýya ata
                            {
                                if (sessionItem.Quantity > sessionItem.Product.Stock)
                                {
                                    sessionItem.Quantity = sessionItem.Product.Stock;
                                    tempStatusMessage = (tempStatusMessage ?? "") + $"'{sessionItem.Product.Name}' ürünü için sepetinizdeki adet stokla ({sessionItem.Product.Stock}) sýnýrlandýrýldý. ";
                                }
                                if (sessionItem.Quantity > 0)
                                {
                                    sessionItem.ApplicationUserId = userId;
                                    sessionItem.SessionId = null; // Artýk kullanýcýya ait, SessionId'yi temizle
                                }
                                else
                                {
                                    _context.CartItems.Remove(sessionItem);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                        // Baþarýlý bir birleþtirmeden sonra, session'daki eski anonim SessionId'yi temizleyebiliriz.
                        // Çünkü artýk tüm sepet kullanýcýnýn UserID'sine baðlý.
                        HttpContext.Session.Remove("SessionId");
                        _logger.LogInformation("Anonim sepet (SessionId: {OldSessionId}) kullanýcýya (UserId: {UserId}) aktarýldý ve session'dan 'SessionId' anahtarý kaldýrýldý.", currentSessionIdForMerge, userId);
                    }
                }
                // Adým 2: Kullanýcýnýn sepetini UserID ile yükle
                query = _context.CartItems.Include(c => c.Product).Where(c => c.ApplicationUserId == userId);
            }
            else // Kullanýcý giriþ yapmamýþ, SessionId ile sepeti yükle
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogWarning("LoadCartItemsAsync: Anonim kullanýcý için SessionId boþ, sepet yüklenemiyor.");
                    Items = new List<CartItemViewModel>();
                    CalculateTotals();
                    return;
                }
                query = _context.CartItems.Include(c => c.Product).Where(c => c.SessionId == sessionId && c.ApplicationUserId == null);
            }

            var cartItemsFromDb = await query
                                    .Where(c => c.Product != null && c.Product.Stock > 0)
                                    .OrderBy(c => c.Product!.Name)
                                    .ToListAsync();
            _logger.LogInformation("Veritabanýndan {Count} adet geçerli ürün yüklendi (UserId: {UserId}, SessionId: {SessionId}).", cartItemsFromDb.Count, userId ?? "ANONYMOUS", sessionId);

            List<CartItemViewModel> loadedItems = new List<CartItemViewModel>();
            bool itemsRemovedOrAdjusted = false;

            foreach (var ci in cartItemsFromDb)
            {
                // Product null kontrolü yukarýdaki Where'de var ama yine de ek güvenlik.
                if (ci.Product == null)
                {
                    _logger.LogWarning("LoadCartItemsAsync: Bir CartItem'in Product'ý null geldi (Id: {CartItemId}). Bu öðe atlanýyor/siliniyor.", ci.Id);
                    _context.CartItems.Remove(ci);
                    itemsRemovedOrAdjusted = true;
                    continue;
                }

                int currentQuantity = ci.Quantity;
                if (ci.Quantity > ci.Product.Stock)
                {
                    currentQuantity = ci.Product.Stock;
                    ci.Quantity = currentQuantity; // Veritabanýndaki adedi de güncelle
                    tempStatusMessage = (tempStatusMessage ?? "") + $"'{ci.Product.Name}' için sepet adedi stokla ({currentQuantity}) güncellendi. ";
                    itemsRemovedOrAdjusted = true;
                    _logger.LogInformation("LoadCartItemsAsync: '{ProductName}' (Id:{ProductId}) adedi stokla ({Stock}) güncellendi.", ci.Product.Name, ci.ProductId, currentQuantity);
                }

                if (currentQuantity <= 0)
                {
                    _logger.LogInformation("LoadCartItemsAsync: '{ProductName}' (Id:{ProductId}) adedi {Quantity} olduðu için sepetten çýkarýlýyor.", ci.Product.Name, ci.ProductId, currentQuantity);
                    _context.CartItems.Remove(ci);
                    itemsRemovedOrAdjusted = true;
                    if (ci.Product.Stock <= 0)
                        tempStatusMessage = (tempStatusMessage ?? "") + $"'{ci.Product.Name}' ürünü stokta kalmadýðý için sepetten çýkarýldý. ";
                    continue;
                }

                loadedItems.Add(new CartItemViewModel
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    ProductImageUrl = ci.Product.ImageUrl,
                    UnitPrice = ci.Product.Price,
                    Quantity = currentQuantity,
                    MaxQuantity = ci.Product.Stock
                });
            }
            Items = loadedItems;

            if (itemsRemovedOrAdjusted)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("LoadCartItemsAsync: Stok veya ürün durumu nedeniyle sepet güncellemeleri veritabanýna kaydedildi.");
            }

            if (!string.IsNullOrEmpty(tempStatusMessage) && string.IsNullOrEmpty(StatusMessage))
            {
                StatusMessage = tempStatusMessage.Trim();
            }

            CalculateTotals();
            _logger.LogInformation("LoadCartItemsAsync tamamlandý. Sepette {ItemCount} çeþit ürün var. Toplamlar: Subtotal={Subtotal}, Shipping={ShippingCost}, Discount={DiscountAmount}, Total={Total}", Items.Count, Subtotal, ShippingCost, DiscountAmount, Total);
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Cart/Index OnGetAsync çaðrýldý.");
            await LoadCartItemsAsync();
            if (TempData.ContainsKey("CouponResponseMessage")) CouponResponseMessage = TempData["CouponResponseMessage"]?.ToString();
            if (TempData.ContainsKey("CouponAppliedSuccessfully")) CouponAppliedSuccessfully = (bool)(TempData["CouponAppliedSuccessfully"] ?? false);
        }

        private void CalculateTotals()
        {
            // Kupon bilgisini session'dan oku (eðer varsa)
            AppliedCouponCode = HttpContext.Session.GetString("AppliedCoupon"); // _httpContextAccessor'a gerek yok.
            DiscountAmount = 0m;
            CouponAppliedSuccessfully = false; // Her hesaplamada sýfýrla, kupon kontrolü yeniden yapsýn
            bool freeShippingByCoupon = false;

            if (!string.IsNullOrEmpty(AppliedCouponCode))
            {
                _logger.LogInformation("CalculateTotals: Uygulanmýþ kupon kodu session'dan okundu: {CouponCode}", AppliedCouponCode);
                // ÖRNEK HARDCODED KUPONLAR (VERÝTABANINDAN VEYA KONFÝGÜRASYONDAN ALINMALI)
                if (AppliedCouponCode.Equals("INDIRIM10", StringComparison.OrdinalIgnoreCase))
                {
                    DiscountAmount = Items.Sum(i => i.TotalPrice) * 0.10m; // Ara toplam üzerinden indirim
                    CouponAppliedSuccessfully = true;
                }
                else if (AppliedCouponCode.Equals("BEDAVAKARGO", StringComparison.OrdinalIgnoreCase))
                {
                    freeShippingByCoupon = true;
                    CouponAppliedSuccessfully = true;
                }
                else
                {
                    // Eðer kupon session'da varsa ama artýk geçerli deðilse, session'dan sil.
                    // Bu mantýk OnPostApplyCouponJsonAsync içinde daha detaylý ele alýnmalý.
                    _logger.LogWarning("CalculateTotals: Session'da bulunan '{CouponCode}' kuponu artýk geçerli deðil veya tanýnmýyor.", AppliedCouponCode);
                    // HttpContext.Session.Remove("AppliedCoupon"); 
                    // AppliedCouponCode = null; 
                    // Bu satýrlarý aktif etmek, geçersiz kuponu otomatik temizler ama kullanýcýya bilgi vermez.
                    // En iyisi bu kontrolü kupon uygulama handler'ýnda yapmak.
                }
            }
            Subtotal = Items.Sum(i => i.TotalPrice); // Ýndirimden önceki ara toplam

            // Örnek Kargo Hesaplamasý (Dinamikleþtirilmeli)
            decimal freeShippingThreshold = 500m;
            decimal standardShippingCost = 49.99m;
            ShippingCost = (Subtotal > 0 && Subtotal < freeShippingThreshold && !freeShippingByCoupon) ? standardShippingCost : 0m;

            Total = Subtotal - DiscountAmount + ShippingCost;
            if (Total < 0) Total = 0;
            _logger.LogInformation("CalculateTotals sonuçlarý: Subtotal={Subtotal}, Discount={DiscountAmount}, Shipping={ShippingCost}, Total={Total}, CouponSuccess={CouponSuccess}", Subtotal, DiscountAmount, ShippingCost, Total, CouponAppliedSuccessfully);
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostUpdateQuantityJsonAsync(int cartItemId, int quantity)
        {
            _logger.LogInformation("AJAX OnPostUpdateQuantityJsonAsync: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, quantity);
            // ... (önceki AJAX handler kodunuz, baþýna ve içine loglar ekleyebilirsiniz) ...
            // ÖNEMLÝ: Bu handler sonunda LoadCartItemsAsync() çaðrýlmalý ki Items, Subtotal, Total vb. güncellensin ve JSON'a eklensin.

            // ---- ÖRNEK BAÞLANGIÇ ---
            string? userId = GetCurrentUserId();
            string sessionId = GetSessionId(false); // Session ID'yi al, yoksa boþ döner, yeni üretme.
            CartItem? item = null;
            string message = "";

            if (!string.IsNullOrEmpty(userId))
            {
                item = await _context.CartItems.Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.ApplicationUserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                item = await _context.CartItems.Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.SessionId == sessionId && ci.ApplicationUserId == null);
            }

            if (item == null || item.Product == null)
            { /* Hata durumu, logla ve uygun JSON dön */
                _logger.LogWarning("UpdateQuantity: Ürün bulunamadý veya ürün detayý bozuk. CartItemId: {CartItemId}", cartItemId);
                await LoadCartItemsAsync(); // Sepeti güncel tut
                return new JsonResult(new
                {
                    success = false,
                    message = "Güncellenecek ürün bulunamadý.",
                    // Güncel sepet bilgilerini yine de gönder
                    subtotal = Subtotal,
                    shippingCost = ShippingCost,
                    discountAmount = DiscountAmount,
                    appliedCouponCode = AppliedCouponCode,
                    total = Total,
                    cartItemCount = Items.Sum(i => i.Quantity),
                    isCartEmpty = IsCartEmpty,
                    removedItemId = item == null ? cartItemId : (int?)null // Eðer item null ise JS'in DOM'dan silmesi için
                });
            }
            // ... (stok kontrolü, adet güncelleme, DB kaydetme ve mesaj oluþturma iþlemleri) ...
            // BURADA GERÇEK GÜNCELLEME MANTIÐINIZ OLMALI
            bool quantityAdjustedDueToStock = false;
            int finalQuantity = quantity;

            if (quantity <= 0) // Adet 0 veya altýysa ürünü sil
            {
                _context.CartItems.Remove(item);
                message = $"'{item.Product.Name}' sepetten kaldýrýldý.";
            }
            else if (quantity > item.Product.Stock)
            {
                finalQuantity = item.Product.Stock;
                if (finalQuantity > 0)
                {
                    item.Quantity = finalQuantity;
                    message = $"'{item.Product.Name}' için stok adedi ({finalQuantity}) ile güncellendi.";
                }
                else
                {
                    _context.CartItems.Remove(item); // Stok 0 ise ürünü sil
                    message = $"'{item.Product.Name}' stokta kalmadýðý için sepetten kaldýrýldý.";
                }
                quantityAdjustedDueToStock = true;
            }
            else
            {
                item.Quantity = finalQuantity;
                message = "Sepet güncellendi.";
            }
            // ---- ÖRNEK BÝTÝÞ ---

            await _context.SaveChangesAsync();
            await LoadCartItemsAsync(); // Tüm toplamlarý ve listeyi yeniden yükle

            return new JsonResult(new
            {
                success = !quantityAdjustedDueToStock || finalQuantity > 0, // Stok ayarý yapýldýysa ama ürün hala sepetteyse success olabilir
                message = message,
                itemId = item?.Id, // item null olabilir
                newQuantity = item?.Quantity, // item null olabilir
                newTotalPriceForItem = item != null && item.Product != null ? (item.Quantity * item.Product.Price) : 0,
                removedItemId = (quantity <= 0 || (quantityAdjustedDueToStock && finalQuantity <= 0)) ? cartItemId : (int?)null,
                subtotal = Subtotal,
                shippingCost = ShippingCost,
                discountAmount = DiscountAmount,
                appliedCouponCode = AppliedCouponCode,
                total = Total,
                cartItemCount = Items.Sum(i => i.Quantity),
                isCartEmpty = IsCartEmpty
            });
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostRemoveItemJsonAsync(int cartItemId)
        {
            _logger.LogInformation("AJAX OnPostRemoveItemJsonAsync: CartItemId={CartItemId}", cartItemId);
            // ... (önceki AJAX handler kodunuz, baþýna ve içine loglar ekleyebilirsiniz) ...
            // ÖNEMLÝ: Bu handler sonunda LoadCartItemsAsync() çaðrýlmalý.

            // ---- ÖRNEK BAÞLANGIÇ ---
            string? userId = GetCurrentUserId();
            string sessionId = GetSessionId(false);
            CartItem? item = null;

            if (!string.IsNullOrEmpty(userId))
            { /* ... user item fetch ... */
                item = await _context.CartItems.Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.ApplicationUserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            { /* ... session item fetch ... */
                item = await _context.CartItems.Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.SessionId == sessionId && ci.ApplicationUserId == null);
            }

            if (item == null)
            { /* Hata durumu */
                _logger.LogWarning("RemoveItem: Ürün bulunamadý. CartItemId: {CartItemId}", cartItemId);
                await LoadCartItemsAsync(); // Sepeti güncel tut
                return new JsonResult(new
                {
                    success = false,
                    message = "Silinecek ürün bulunamadý.",
                    subtotal = Subtotal,
                    shippingCost = ShippingCost,
                    discountAmount = DiscountAmount,
                    appliedCouponCode = AppliedCouponCode,
                    total = Total,
                    cartItemCount = Items.Sum(i => i.Quantity),
                    isCartEmpty = IsCartEmpty
                });
            }
            var productName = item.Product?.Name ?? "Bir ürün";
            _context.CartItems.Remove(item);
            // ---- ÖRNEK BÝTÝÞ ---

            await _context.SaveChangesAsync();
            await LoadCartItemsAsync();

            return new JsonResult(new
            {
                success = true,
                message = $"'{productName}' sepetten kaldýrýldý.",
                removedItemId = cartItemId,
                subtotal = Subtotal,
                shippingCost = ShippingCost,
                discountAmount = DiscountAmount,
                appliedCouponCode = AppliedCouponCode,
                total = Total,
                cartItemCount = Items.Sum(i => i.Quantity),
                isCartEmpty = IsCartEmpty
            });
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostApplyCouponJsonAsync(string? couponCode)
        {
            _logger.LogInformation("AJAX OnPostApplyCouponJsonAsync: CouponCode={CouponCode}", couponCode);
            // ... (önceki AJAX handler kodunuz, baþýna ve içine loglar ekleyebilirsiniz) ...
            // ÖNEMLÝ: Bu handler sonunda LoadCartItemsAsync() çaðrýlmalý.

            // ---- ÖRNEK BAÞLANGIÇ ---
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                HttpContext.Session.Remove("AppliedCoupon");
                CouponResponseMessage = "Lütfen bir kupon kodu girin.";
                CouponAppliedSuccessfully = false;
            }
            else
            {
                // GERÇEK KUPON KONTROLÜ VERÝTABANINDAN YAPILMALI
                if (couponCode.Equals("INDIRIM10", StringComparison.OrdinalIgnoreCase) || couponCode.Equals("BEDAVAKARGO", StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext.Session.SetString("AppliedCoupon", couponCode);
                    CouponResponseMessage = $"'{couponCode}' kuponu baþarýyla uygulandý!";
                    CouponAppliedSuccessfully = true;
                }
                else
                {
                    HttpContext.Session.Remove("AppliedCoupon");
                    CouponResponseMessage = "Geçersiz kupon kodu.";
                    CouponAppliedSuccessfully = false;
                }
            }
            // ---- ÖRNEK BÝTÝÞ ---

            await LoadCartItemsAsync(); // Kupon durumuna göre toplamlarý yeniden hesapla

            return new JsonResult(new
            {
                success = CouponAppliedSuccessfully,
                message = CouponResponseMessage,
                subtotal = Subtotal,
                shippingCost = ShippingCost,
                discountAmount = DiscountAmount,
                appliedCouponCode = AppliedCouponCode, // Bu LoadCartItems -> CalculateTotals içinde session'dan güncellenir.
                total = Total
            });
        }
    }
}