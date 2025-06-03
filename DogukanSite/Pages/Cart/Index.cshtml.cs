using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // Session i�in
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Logging i�in eklendi

namespace DogukanSite.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        // IHttpContextAccessor'a burada do�rudan ihtiyac�m�z olmayabilir, PageModel'�n kendi HttpContext'i var.
        // Ama kullan�yorsan�z kalabilir.
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(DogukanSiteContext context,
                          UserManager<ApplicationUser> userManager,
                          IHttpContextAccessor httpContextAccessor, // E�er GetSessionId'de HttpContext'i buradan al�yorsan�z
                          ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor; // E�er kullan�l�yorsa
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
            public string ProductName { get; set; } = "�r�n Ad� Yok";
            public string? ProductImageUrl { get; set; }
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal TotalPrice => UnitPrice * Quantity;
            public int MaxQuantity { get; set; }
        }

        private string? GetCurrentUserId()
        {
            // Bu metod PageModel.User �zerinden �al��t��� i�in IHttpContextAccessor'a gerek duymaz.
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return _userManager.GetUserId(User);
            }
            return null;
        }

        // �NEML�: Bu GetSessionId metodunu Order/CreateModel.cs i�indekiyle ayn� mant�kta ve robustlukta yapal�m.
        private string GetSessionId(bool createIfNull = true)
        {
            var currentPath = HttpContext.Request.Path; // Loglama i�in
            _logger.LogInformation("CartModel.GetSessionId �a�r�ld�. Request Path: {Path}, createIfNull: {CreateIfNull}", currentPath, createIfNull);

            if (HttpContext.Session == null)
            {
                _logger.LogError("CartModel.GetSessionId: HttpContext.Session IS NULL! Session middleware (app.UseSession()) Program.cs'te do�ru yap�land�r�lmam�� veya �a�r�lmam�� olabilir. Path: {Path}", currentPath);
                throw new InvalidOperationException("HttpContext.Session is null. Session middleware do�ru yap�land�r�lmam�� olabilir.");
            }

            if (!HttpContext.Session.IsAvailable)
            {
                _logger.LogWarning("CartModel.GetSessionId: HttpContext.Session.IsAvailable == false. Session y�klenemedi veya kullan�lam�yor. Path: {Path}", currentPath);
                // OrderModel'da oldu�u gibi burada da hata f�rlatmak daha do�ru olacakt�r.
                // E�er createIfNull false ise ve session yoksa bo� string d�nmek yerine null d�nmek daha anlaml� olabilir.
                if (!createIfNull) return string.Empty; // veya null, �a��ran yerin null kontrol�ne g�re
                throw new InvalidOperationException($"Session is not available for path {currentPath}. Ensure session is configured and enabled, and app.UseSession() is correctly placed in Program.cs.");
            }

            string? sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId) && createIfNull)
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
                _logger.LogInformation("CartModel.GetSessionId: Yeni SessionId �retildi ve set edildi: {SessionId}. Path: {Path}", sessionId, currentPath);
            }
            else if (string.IsNullOrEmpty(sessionId) && !createIfNull)
            {
                _logger.LogWarning("CartModel.GetSessionId: SessionId null/empty ve createIfNull false. Bo� string d�n�l�yor. Path: {Path}", currentPath);
                return string.Empty; // veya null
            }
            else
            {
                _logger.LogInformation("CartModel.GetSessionId: Mevcut SessionId al�nd�: {SessionId}. Path: {Path}", sessionId, currentPath);
            }
            return sessionId ?? string.Empty; // Null ise bo� string d�n (string.IsNullOrEmpty kontrol� nedeniyle)
        }


        private async Task LoadCartItemsAsync()
        {
            _logger.LogInformation("CartModel.LoadCartItemsAsync �a�r�ld�.");
            string? userId = GetCurrentUserId();
            // Kullan�c� giri� yapm��sa bile, birle�tirme i�in anonim SessionId'ye ihtiyac�m�z olabilir.
            // E�er kullan�c� giri� yapm�� ve session'da ID yoksa, yeni ID olu�turmamal�y�z, ��nk� sepeti UserID'ye ba�l� olmal�.
            // Bu nedenle, createIfNull parametresini userId'nin durumuna g�re ayarl�yoruz.
            string sessionId = GetSessionId(createIfNull: string.IsNullOrEmpty(userId));
            _logger.LogInformation("LoadCartItemsAsync: UserId={UserId}, SessionId={SessionId}", userId ?? "ANONYMOUS", sessionId);

            string? tempStatusMessage = null;
            IQueryable<CartItem> query;

            if (!string.IsNullOrEmpty(userId)) // Kullan�c� giri� yapm��
            {
                // Ad�m 1: E�er session'da bir "SessionId" varsa (kullan�c� giri� yapmadan �nce anonim sepeti olabilir),
                // bu anonim sepetteki �r�nleri kullan�c�n�n veritaban�ndaki sepetine birle�tir.
                string currentSessionIdForMerge = HttpContext.Session.GetString("SessionId") ?? string.Empty; // Sadece oku, �retme.

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
                                tempStatusMessage = (tempStatusMessage ?? "") + $"'{sessionItem.Product?.Name ?? "Bilinmeyen bir �r�n"}' stokta kalmad��� i�in sepetinizden ��kar�ld�. ";
                                continue;
                            }

                            var userCartItem = await _context.CartItems
                                .FirstOrDefaultAsync(ci => ci.ApplicationUserId == userId && ci.ProductId == sessionItem.ProductId);

                            if (userCartItem != null) // Kullan�c�n�n sepetinde bu �r�n zaten var, adetleri birle�tir
                            {
                                int newQuantityForUserItem = userCartItem.Quantity + sessionItem.Quantity;
                                if (newQuantityForUserItem > sessionItem.Product.Stock)
                                {
                                    newQuantityForUserItem = sessionItem.Product.Stock;
                                    tempStatusMessage = (tempStatusMessage ?? "") + $"'{sessionItem.Product.Name}' �r�n� i�in sepetinizdeki toplam adet stokla ({sessionItem.Product.Stock}) s�n�rland�r�ld�. ";
                                }
                                if (newQuantityForUserItem > 0) userCartItem.Quantity = newQuantityForUserItem;
                                else _context.CartItems.Remove(userCartItem);
                                _context.CartItems.Remove(sessionItem); // Anonim olan� sil
                            }
                            else // Kullan�c�n�n sepetinde bu �r�n yok, anonim �r�n� kullan�c�ya ata
                            {
                                if (sessionItem.Quantity > sessionItem.Product.Stock)
                                {
                                    sessionItem.Quantity = sessionItem.Product.Stock;
                                    tempStatusMessage = (tempStatusMessage ?? "") + $"'{sessionItem.Product.Name}' �r�n� i�in sepetinizdeki adet stokla ({sessionItem.Product.Stock}) s�n�rland�r�ld�. ";
                                }
                                if (sessionItem.Quantity > 0)
                                {
                                    sessionItem.ApplicationUserId = userId;
                                    sessionItem.SessionId = null; // Art�k kullan�c�ya ait, SessionId'yi temizle
                                }
                                else
                                {
                                    _context.CartItems.Remove(sessionItem);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                        // Ba�ar�l� bir birle�tirmeden sonra, session'daki eski anonim SessionId'yi temizleyebiliriz.
                        // ��nk� art�k t�m sepet kullan�c�n�n UserID'sine ba�l�.
                        HttpContext.Session.Remove("SessionId");
                        _logger.LogInformation("Anonim sepet (SessionId: {OldSessionId}) kullan�c�ya (UserId: {UserId}) aktar�ld� ve session'dan 'SessionId' anahtar� kald�r�ld�.", currentSessionIdForMerge, userId);
                    }
                }
                // Ad�m 2: Kullan�c�n�n sepetini UserID ile y�kle
                query = _context.CartItems.Include(c => c.Product).Where(c => c.ApplicationUserId == userId);
            }
            else // Kullan�c� giri� yapmam��, SessionId ile sepeti y�kle
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogWarning("LoadCartItemsAsync: Anonim kullan�c� i�in SessionId bo�, sepet y�klenemiyor.");
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
            _logger.LogInformation("Veritaban�ndan {Count} adet ge�erli �r�n y�klendi (UserId: {UserId}, SessionId: {SessionId}).", cartItemsFromDb.Count, userId ?? "ANONYMOUS", sessionId);

            List<CartItemViewModel> loadedItems = new List<CartItemViewModel>();
            bool itemsRemovedOrAdjusted = false;

            foreach (var ci in cartItemsFromDb)
            {
                // Product null kontrol� yukar�daki Where'de var ama yine de ek g�venlik.
                if (ci.Product == null)
                {
                    _logger.LogWarning("LoadCartItemsAsync: Bir CartItem'in Product'� null geldi (Id: {CartItemId}). Bu ��e atlan�yor/siliniyor.", ci.Id);
                    _context.CartItems.Remove(ci);
                    itemsRemovedOrAdjusted = true;
                    continue;
                }

                int currentQuantity = ci.Quantity;
                if (ci.Quantity > ci.Product.Stock)
                {
                    currentQuantity = ci.Product.Stock;
                    ci.Quantity = currentQuantity; // Veritaban�ndaki adedi de g�ncelle
                    tempStatusMessage = (tempStatusMessage ?? "") + $"'{ci.Product.Name}' i�in sepet adedi stokla ({currentQuantity}) g�ncellendi. ";
                    itemsRemovedOrAdjusted = true;
                    _logger.LogInformation("LoadCartItemsAsync: '{ProductName}' (Id:{ProductId}) adedi stokla ({Stock}) g�ncellendi.", ci.Product.Name, ci.ProductId, currentQuantity);
                }

                if (currentQuantity <= 0)
                {
                    _logger.LogInformation("LoadCartItemsAsync: '{ProductName}' (Id:{ProductId}) adedi {Quantity} oldu�u i�in sepetten ��kar�l�yor.", ci.Product.Name, ci.ProductId, currentQuantity);
                    _context.CartItems.Remove(ci);
                    itemsRemovedOrAdjusted = true;
                    if (ci.Product.Stock <= 0)
                        tempStatusMessage = (tempStatusMessage ?? "") + $"'{ci.Product.Name}' �r�n� stokta kalmad��� i�in sepetten ��kar�ld�. ";
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
                _logger.LogInformation("LoadCartItemsAsync: Stok veya �r�n durumu nedeniyle sepet g�ncellemeleri veritaban�na kaydedildi.");
            }

            if (!string.IsNullOrEmpty(tempStatusMessage) && string.IsNullOrEmpty(StatusMessage))
            {
                StatusMessage = tempStatusMessage.Trim();
            }

            CalculateTotals();
            _logger.LogInformation("LoadCartItemsAsync tamamland�. Sepette {ItemCount} �e�it �r�n var. Toplamlar: Subtotal={Subtotal}, Shipping={ShippingCost}, Discount={DiscountAmount}, Total={Total}", Items.Count, Subtotal, ShippingCost, DiscountAmount, Total);
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Cart/Index OnGetAsync �a�r�ld�.");
            await LoadCartItemsAsync();
            if (TempData.ContainsKey("CouponResponseMessage")) CouponResponseMessage = TempData["CouponResponseMessage"]?.ToString();
            if (TempData.ContainsKey("CouponAppliedSuccessfully")) CouponAppliedSuccessfully = (bool)(TempData["CouponAppliedSuccessfully"] ?? false);
        }

        private void CalculateTotals()
        {
            // Kupon bilgisini session'dan oku (e�er varsa)
            AppliedCouponCode = HttpContext.Session.GetString("AppliedCoupon"); // _httpContextAccessor'a gerek yok.
            DiscountAmount = 0m;
            CouponAppliedSuccessfully = false; // Her hesaplamada s�f�rla, kupon kontrol� yeniden yaps�n
            bool freeShippingByCoupon = false;

            if (!string.IsNullOrEmpty(AppliedCouponCode))
            {
                _logger.LogInformation("CalculateTotals: Uygulanm�� kupon kodu session'dan okundu: {CouponCode}", AppliedCouponCode);
                // �RNEK HARDCODED KUPONLAR (VER�TABANINDAN VEYA KONF�G�RASYONDAN ALINMALI)
                if (AppliedCouponCode.Equals("INDIRIM10", StringComparison.OrdinalIgnoreCase))
                {
                    DiscountAmount = Items.Sum(i => i.TotalPrice) * 0.10m; // Ara toplam �zerinden indirim
                    CouponAppliedSuccessfully = true;
                }
                else if (AppliedCouponCode.Equals("BEDAVAKARGO", StringComparison.OrdinalIgnoreCase))
                {
                    freeShippingByCoupon = true;
                    CouponAppliedSuccessfully = true;
                }
                else
                {
                    // E�er kupon session'da varsa ama art�k ge�erli de�ilse, session'dan sil.
                    // Bu mant�k OnPostApplyCouponJsonAsync i�inde daha detayl� ele al�nmal�.
                    _logger.LogWarning("CalculateTotals: Session'da bulunan '{CouponCode}' kuponu art�k ge�erli de�il veya tan�nm�yor.", AppliedCouponCode);
                    // HttpContext.Session.Remove("AppliedCoupon"); 
                    // AppliedCouponCode = null; 
                    // Bu sat�rlar� aktif etmek, ge�ersiz kuponu otomatik temizler ama kullan�c�ya bilgi vermez.
                    // En iyisi bu kontrol� kupon uygulama handler'�nda yapmak.
                }
            }
            Subtotal = Items.Sum(i => i.TotalPrice); // �ndirimden �nceki ara toplam

            // �rnek Kargo Hesaplamas� (Dinamikle�tirilmeli)
            decimal freeShippingThreshold = 500m;
            decimal standardShippingCost = 49.99m;
            ShippingCost = (Subtotal > 0 && Subtotal < freeShippingThreshold && !freeShippingByCoupon) ? standardShippingCost : 0m;

            Total = Subtotal - DiscountAmount + ShippingCost;
            if (Total < 0) Total = 0;
            _logger.LogInformation("CalculateTotals sonu�lar�: Subtotal={Subtotal}, Discount={DiscountAmount}, Shipping={ShippingCost}, Total={Total}, CouponSuccess={CouponSuccess}", Subtotal, DiscountAmount, ShippingCost, Total, CouponAppliedSuccessfully);
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostUpdateQuantityJsonAsync(int cartItemId, int quantity)
        {
            _logger.LogInformation("AJAX OnPostUpdateQuantityJsonAsync: CartItemId={CartItemId}, Quantity={Quantity}", cartItemId, quantity);
            // ... (�nceki AJAX handler kodunuz, ba��na ve i�ine loglar ekleyebilirsiniz) ...
            // �NEML�: Bu handler sonunda LoadCartItemsAsync() �a�r�lmal� ki Items, Subtotal, Total vb. g�ncellensin ve JSON'a eklensin.

            // ---- �RNEK BA�LANGI� ---
            string? userId = GetCurrentUserId();
            string sessionId = GetSessionId(false); // Session ID'yi al, yoksa bo� d�ner, yeni �retme.
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
            { /* Hata durumu, logla ve uygun JSON d�n */
                _logger.LogWarning("UpdateQuantity: �r�n bulunamad� veya �r�n detay� bozuk. CartItemId: {CartItemId}", cartItemId);
                await LoadCartItemsAsync(); // Sepeti g�ncel tut
                return new JsonResult(new
                {
                    success = false,
                    message = "G�ncellenecek �r�n bulunamad�.",
                    // G�ncel sepet bilgilerini yine de g�nder
                    subtotal = Subtotal,
                    shippingCost = ShippingCost,
                    discountAmount = DiscountAmount,
                    appliedCouponCode = AppliedCouponCode,
                    total = Total,
                    cartItemCount = Items.Sum(i => i.Quantity),
                    isCartEmpty = IsCartEmpty,
                    removedItemId = item == null ? cartItemId : (int?)null // E�er item null ise JS'in DOM'dan silmesi i�in
                });
            }
            // ... (stok kontrol�, adet g�ncelleme, DB kaydetme ve mesaj olu�turma i�lemleri) ...
            // BURADA GER�EK G�NCELLEME MANTI�INIZ OLMALI
            bool quantityAdjustedDueToStock = false;
            int finalQuantity = quantity;

            if (quantity <= 0) // Adet 0 veya alt�ysa �r�n� sil
            {
                _context.CartItems.Remove(item);
                message = $"'{item.Product.Name}' sepetten kald�r�ld�.";
            }
            else if (quantity > item.Product.Stock)
            {
                finalQuantity = item.Product.Stock;
                if (finalQuantity > 0)
                {
                    item.Quantity = finalQuantity;
                    message = $"'{item.Product.Name}' i�in stok adedi ({finalQuantity}) ile g�ncellendi.";
                }
                else
                {
                    _context.CartItems.Remove(item); // Stok 0 ise �r�n� sil
                    message = $"'{item.Product.Name}' stokta kalmad��� i�in sepetten kald�r�ld�.";
                }
                quantityAdjustedDueToStock = true;
            }
            else
            {
                item.Quantity = finalQuantity;
                message = "Sepet g�ncellendi.";
            }
            // ---- �RNEK B�T�� ---

            await _context.SaveChangesAsync();
            await LoadCartItemsAsync(); // T�m toplamlar� ve listeyi yeniden y�kle

            return new JsonResult(new
            {
                success = !quantityAdjustedDueToStock || finalQuantity > 0, // Stok ayar� yap�ld�ysa ama �r�n hala sepetteyse success olabilir
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
            // ... (�nceki AJAX handler kodunuz, ba��na ve i�ine loglar ekleyebilirsiniz) ...
            // �NEML�: Bu handler sonunda LoadCartItemsAsync() �a�r�lmal�.

            // ---- �RNEK BA�LANGI� ---
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
                _logger.LogWarning("RemoveItem: �r�n bulunamad�. CartItemId: {CartItemId}", cartItemId);
                await LoadCartItemsAsync(); // Sepeti g�ncel tut
                return new JsonResult(new
                {
                    success = false,
                    message = "Silinecek �r�n bulunamad�.",
                    subtotal = Subtotal,
                    shippingCost = ShippingCost,
                    discountAmount = DiscountAmount,
                    appliedCouponCode = AppliedCouponCode,
                    total = Total,
                    cartItemCount = Items.Sum(i => i.Quantity),
                    isCartEmpty = IsCartEmpty
                });
            }
            var productName = item.Product?.Name ?? "Bir �r�n";
            _context.CartItems.Remove(item);
            // ---- �RNEK B�T�� ---

            await _context.SaveChangesAsync();
            await LoadCartItemsAsync();

            return new JsonResult(new
            {
                success = true,
                message = $"'{productName}' sepetten kald�r�ld�.",
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
            // ... (�nceki AJAX handler kodunuz, ba��na ve i�ine loglar ekleyebilirsiniz) ...
            // �NEML�: Bu handler sonunda LoadCartItemsAsync() �a�r�lmal�.

            // ---- �RNEK BA�LANGI� ---
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                HttpContext.Session.Remove("AppliedCoupon");
                CouponResponseMessage = "L�tfen bir kupon kodu girin.";
                CouponAppliedSuccessfully = false;
            }
            else
            {
                // GER�EK KUPON KONTROL� VER�TABANINDAN YAPILMALI
                if (couponCode.Equals("INDIRIM10", StringComparison.OrdinalIgnoreCase) || couponCode.Equals("BEDAVAKARGO", StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext.Session.SetString("AppliedCoupon", couponCode);
                    CouponResponseMessage = $"'{couponCode}' kuponu ba�ar�yla uyguland�!";
                    CouponAppliedSuccessfully = true;
                }
                else
                {
                    HttpContext.Session.Remove("AppliedCoupon");
                    CouponResponseMessage = "Ge�ersiz kupon kodu.";
                    CouponAppliedSuccessfully = false;
                }
            }
            // ---- �RNEK B�T�� ---

            await LoadCartItemsAsync(); // Kupon durumuna g�re toplamlar� yeniden hesapla

            return new JsonResult(new
            {
                success = CouponAppliedSuccessfully,
                message = CouponResponseMessage,
                subtotal = Subtotal,
                shippingCost = ShippingCost,
                discountAmount = DiscountAmount,
                appliedCouponCode = AppliedCouponCode, // Bu LoadCartItems -> CalculateTotals i�inde session'dan g�ncellenir.
                total = Total
            });
        }
    }
}