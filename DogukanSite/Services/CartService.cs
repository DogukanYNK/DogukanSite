using DogukanSite.Data;
using DogukanSite.Models;
using DogukanSite.Pages.Cart;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DogukanSite.Services
{
    public class CartService : ICartService
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartService> _logger;

        public CartService(DogukanSiteContext context,
                         UserManager<ApplicationUser> userManager,
                         IHttpContextAccessor httpContextAccessor,
                         ILogger<CartService> logger)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private HttpContext HttpContext => _httpContextAccessor.HttpContext;
        private ClaimsPrincipal User => HttpContext?.User;

        private string GetCurrentUserId() => User == null ? null : _userManager.GetUserId(User);

        private string GetSessionId(bool createIfNull = true)
        {
            if (HttpContext == null) return null;

            string sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId) && createIfNull)
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
                _logger.LogInformation("Yeni SessionId oluşturuldu: {SessionId}", sessionId);
            }
            return sessionId;
        }

        public async Task<CartViewModel> GetCartViewModelAsync()
        {
            var cartItems = await GetCartItemsAsync();
            return await BuildCartViewModel(cartItems);
        }

        private async Task<List<CartItem>> GetCartItemsAsync()
        {
            _logger.LogWarning("--- CartService.GetCartItemsAsync başlıyor ---");
            string userId = GetCurrentUserId();
            _logger.LogWarning("Mevcut Kullanıcı ID'si (UserId): {UserId}", string.IsNullOrEmpty(userId) ? "BOŞ" : userId);

            IQueryable<CartItem> query;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Giriş yapmış kullanıcı yolu seçildi. UserId: {UserId} için sorgu hazırlanıyor.", userId);
                query = _context.CartItems.Include(c => c.Product).Where(c => c.ApplicationUserId == userId);
            }
            else
            {
                _logger.LogWarning("Misafir kullanıcı yolu seçildi.");
                string sessionId = GetSessionId(createIfNull: false);
                _logger.LogWarning("Mevcut Oturum ID'si (SessionId): {SessionId}", string.IsNullOrEmpty(sessionId) ? "BOŞ" : sessionId);

                if (string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogWarning("SessionId bulunamadığı için boş sepet dönülüyor.");
                    return new List<CartItem>();
                }

                query = _context.CartItems.Include(c => c.Product).Where(c => c.SessionId == sessionId && c.ApplicationUserId == null);
            }

            var items = await query.Where(ci => ci.Product != null && ci.Product.Stock > 0).ToListAsync();
            _logger.LogWarning("Sorgu sonucu {ItemCount} adet ürün bulundu.", items.Count);
            _logger.LogWarning("--- CartService.GetCartItemsAsync bitti ---");
            return items;
        }

        // Diğer tüm metodları (BuildCartViewModel, ApplyCouponAsync vb.) buraya önceki adımdaki gibi ekleyin.
        // ... (Bu metodlar projenin işleyişi için gereklidir)
        public async Task MergeCartsOnLoginAsync()
        {
            string userId = GetCurrentUserId();
            string sessionId = GetSessionId(false);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId)) return;

            // DEĞİŞTİRİLDİ: Stok bilgisine erişmek için .Include(ci => ci.Product) eklendi.
            var sessionCartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.SessionId == sessionId)
                .ToListAsync();

            if (!sessionCartItems.Any()) return;

            var userCartItems = await _context.CartItems.Where(ci => ci.ApplicationUserId == userId).ToListAsync();

            foreach (var sessionItem in sessionCartItems)
            {
                // Misafir sepetindeki ürünün stok bilgisi yoksa veya stokta kalmadıysa bu ürünü atla/sil
                if (sessionItem.Product == null || sessionItem.Product.Stock <= 0)
                {
                    _context.CartItems.Remove(sessionItem);
                    continue;
                }

                var existingUserItem = userCartItems.FirstOrDefault(i => i.ProductId == sessionItem.ProductId);

                if (existingUserItem != null) // Kullanıcının sepetinde bu ürün zaten varsa
                {
                    int newQuantity = existingUserItem.Quantity + sessionItem.Quantity;

                    // YENİ: Stok kontrolü eklendi.
                    if (newQuantity > sessionItem.Product.Stock)
                    {
                        newQuantity = sessionItem.Product.Stock; // Adedi stokla sınırla
                    }
                    existingUserItem.Quantity = newQuantity;
                    _context.CartItems.Remove(sessionItem); // Misafir sepetindeki kopyayı sil
                }
                else // Kullanıcının sepetinde bu ürün yoksa, misafir sepetindeki ürünü transfer et
                {
                    // YENİ: Transfer etmeden önce stok kontrolü eklendi.
                    if (sessionItem.Quantity > sessionItem.Product.Stock)
                    {
                        sessionItem.Quantity = sessionItem.Product.Stock; // Adedi stokla sınırla
                    }

                    sessionItem.SessionId = null; // Session bağını kopar
                    sessionItem.ApplicationUserId = userId; // Kullanıcıya bağla
                }
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("SessionId"); // Misafir sepeti session'ını temizle
        }

        public async Task<int> GetCartItemCountAsync()
        {
            var items = await GetCartItemsAsync();
            return items.Sum(i => i.Quantity);
        }

        public async Task<CartViewModel> UpdateItemQuantityAsync(int cartItemId, int quantity)
        {
            var item = await FindCartItemByIdAsync(cartItemId);
            if (item == null) { return await GetCartViewModelAsync(); }
            if (quantity <= 0) { _context.CartItems.Remove(item); } else if (item.Product != null && quantity > item.Product.Stock) { item.Quantity = item.Product.Stock; } else { item.Quantity = quantity; }
            await _context.SaveChangesAsync(); return await GetCartViewModelAsync();
        }

        public async Task<CartViewModel> RemoveItemAsync(int cartItemId)
        {
            var item = await FindCartItemByIdAsync(cartItemId);
            if (item != null) { _context.CartItems.Remove(item); await _context.SaveChangesAsync(); }
            return await GetCartViewModelAsync();
        }

        public async Task<CartViewModel> ApplyCouponAsync(string couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode)) { HttpContext.Session.Remove("AppliedCoupon"); } else { HttpContext.Session.SetString("AppliedCoupon", couponCode); }
            return await GetCartViewModelAsync();
        }
        public async Task<CartViewModel> RemoveCouponAsync()
        {
            _logger.LogInformation("Uygulanan kupon kaldırılıyor.");
            HttpContext.Session.Remove("AppliedCoupon");
            return await GetCartViewModelAsync();
        }
        private async Task<CartItem> FindCartItemByIdAsync(int cartItemId)
        {
            string userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId)) { return await _context.CartItems.Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.ApplicationUserId == userId); } else { string sessionId = GetSessionId(false); return await _context.CartItems.Include(ci => ci.Product).FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.SessionId == sessionId); }
        }

        private async Task<CartViewModel> BuildCartViewModel(List<CartItem> items)
        {
            var viewModel = new CartViewModel();
            bool databaseNeedsUpdate = false;

            var itemsToRemove = new List<CartItem>();

            foreach (var item in items)
            {
                // İlişkili ürün silinmişse veya stok tamamen tükenmişse, bu sepet öğesini silinmek üzere işaretle
                if (item.Product == null || item.Product.Stock <= 0)
                {
                    itemsToRemove.Add(item);
                    databaseNeedsUpdate = true;
                    continue; // Bu ürünü ViewModel'e eklemeden bir sonrakine geç
                }

                // Eğer sepetteki adet, mevcut stoktan fazlaysa, adedi stokla sınırla
                if (item.Quantity > item.Product.Stock)
                {
                    item.Quantity = item.Product.Stock;
                    databaseNeedsUpdate = true; // Veritabanını güncellememiz gerektiğini işaretle
                }

                // Sadece geçerli ve adedi ayarlanmış ürünleri ViewModel'e ekle
                viewModel.Items.Add(new CartItemViewModel
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductImageUrl = item.Product.ImageUrl,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity,
                    MaxQuantity = item.Product.Stock
                });
            }

            // Eğer silinecek ürünler varsa, onları veritabanı context'inden kaldır
            if (itemsToRemove.Any())
            {
                _context.CartItems.RemoveRange(itemsToRemove);
            }

            // Eğer herhangi bir güncelleme (silme veya adet düzeltme) yapıldıysa, tüm değişiklikleri tek seferde kaydet
            if (databaseNeedsUpdate)
            {
                _logger.LogInformation("Stok durumu nedeniyle sepet öğeleri veritabanında güncellendi veya silindi.");
                await _context.SaveChangesAsync();
            }

            // --- Buradan sonraki toplam ve kupon hesaplama mantığı aynı kalacak ---
            // Bu hesaplamalar artık sadece geçerli ürünleri içeren viewModel.Items üzerinden yapılacak.

            string appliedCouponCode = HttpContext.Session.GetString("AppliedCoupon");

            viewModel.Subtotal = viewModel.Items.Sum(i => i.TotalPrice);

            if (!string.IsNullOrEmpty(appliedCouponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == appliedCouponCode.ToUpper() && c.IsActive);
                if (coupon != null && coupon.StartDate <= DateTime.UtcNow && coupon.EndDate >= DateTime.UtcNow && (coupon.UsageLimit == null || coupon.UsageCount < coupon.UsageLimit))
                {
                    viewModel.AppliedCouponCode = coupon.Code;
                    if (coupon.Type == DiscountType.Percentage) { viewModel.DiscountAmount = viewModel.Subtotal * (coupon.Value / 100m); }
                    else if (coupon.Type == DiscountType.FixedAmount) { viewModel.DiscountAmount = coupon.Value; }
                    viewModel.CouponResponseMessage = $"'{coupon.Code}' kuponu başarıyla uygulandı.";
                    viewModel.CouponAppliedSuccessfully = true;
                }
                else
                {
                    viewModel.CouponResponseMessage = "Geçersiz veya süresi dolmuş kupon kodu.";
                    HttpContext.Session.Remove("AppliedCoupon");
                }
            }

            decimal freeShippingThreshold = 500m;
            decimal standardShippingCost = 49.99m;
            viewModel.ShippingCost = (viewModel.Subtotal > 0 && viewModel.Subtotal < freeShippingThreshold && viewModel.DiscountAmount < viewModel.Subtotal) ? standardShippingCost : 0m;

            viewModel.Total = viewModel.Subtotal - viewModel.DiscountAmount + viewModel.ShippingCost;
            if (viewModel.Total < 0) viewModel.Total = 0;

            return viewModel;
        }   
    }
}
