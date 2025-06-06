using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages
{
    [ValidateAntiForgeryToken]
    public class IndexModel : PageModel
    {
        // --- MEVCUT KODUNUZ OLDUÐU GÝBÝ KALIYOR ---
        private readonly DogukanSiteContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            DogukanSiteContext context,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _logger = logger;
        }

        public HashSet<int> UserFavoriteProductIds { get; set; } = new HashSet<int>();

        // Bu listeleri yeni mantýkla dolduracaðýz
        public List<Product> NewArrivals { get; set; } = new List<Product>();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<CategoryTeaser> FeaturedCategories { get; set; } = new List<CategoryTeaser>();

        // --- YARDIMCI METODLARINIZ OLDUÐU GÝBÝ KALIYOR ---
        private string? GetCurrentUserId()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return _userManager.GetUserId(User);
            }
            return null;
        }

        // ... GetSessionId ve diðer yardýmcý metotlarýnýz burada kalacak ...
        private string GetSessionId(bool createIfNull = true)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.Session == null)
            {
                _logger.LogWarning("HttpContext or Session is null in GetSessionId for IndexModel.");
                return createIfNull ? Guid.NewGuid().ToString() : string.Empty;
            }
            var sessionId = httpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId) && createIfNull)
            {
                sessionId = Guid.NewGuid().ToString();
                httpContext.Session.SetString("SessionId", sessionId);
            }
            return sessionId ?? string.Empty;
        }


        // === SADECE BU METODU YENÝ MANTIKLA GÜNCELLÝYORUZ ===
        // === SADECE BU METODU YENÝ HALÝYLE GÜNCELLEYÝN ===
        public async Task OnGetAsync()
        {
            // 1. Mevcut favori bilgisini çekme (kodunuzdan alýndý, bu önemli!)
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                UserFavoriteProductIds = (await _context.Favorites
                    .AsNoTracking()
                    .Where(f => f.ApplicationUserId == userId)
                    .Select(f => f.ProductId)
                    .ToListAsync())
                    .ToHashSet();
            }

            // 2. "Yeni Gelenler" bölümünü doldurma (DÜZELTÝLMÝÞ HALÝ)
            NewArrivals = await _context.Products // .Product yerine .Products
                .Where(p => p.IsNewArrival)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            // 3. "Öne Çýkanlar" bölümünü doldurma (DÜZELTÝLMÝÞ HALÝ)
            FeaturedProducts = await _context.Products // .Product yerine .Products
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.Id)
                .Take(4)
                .AsNoTracking()
                .ToListAsync();

            // 4. Öne Çýkan Kategoriler (þimdilik manuel kalýyor)
            FeaturedCategories = new List<CategoryTeaser>
    {
        new CategoryTeaser { Name = "Elektronik", ImageUrl = "/images/categories/electronics.jpg", PageUrl = "/Products/Index?category=Elektronik" },
        new CategoryTeaser { Name = "Giyim", ImageUrl = "/images/categories/fashion.jpg", PageUrl = "/Products/Index?category=Giyim" },
        new CategoryTeaser { Name = "Ev & Yaþam", ImageUrl = "/images/categories/home-living.jpg", PageUrl = "/Products/Index?category=EvYasam" },
        new CategoryTeaser { Name = "Kozmetik", ImageUrl = "/images/categories/cosmetics.jpg", PageUrl = "/Products/Index?category=Kozmetik" }
    };

            // Bu satýr da önemli, favori kalplerinin doðru çalýþmasý için
            ViewData["UserFavoriteProductIds"] = UserFavoriteProductIds;
        }

        // === SEPETE EKLEME, FAVORÝ EKLEME GÝBÝ DÝÐER TÜM METOTLARINIZ OLDUÐU GÝBÝ KALIYOR ===

        public async Task<JsonResult> OnGetCartCountAsync()
        {
            // ... mevcut kodunuz ...
            try
            {
                string? userId = GetCurrentUserId();
                int count = 0;
                if (!string.IsNullOrEmpty(userId))
                {
                    count = await _context.CartItems.Where(c => c.ApplicationUserId == userId).SumAsync(c => (int?)c.Quantity) ?? 0;
                }
                else
                {
                    var sessionId = GetSessionId(createIfNull: false);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        count = await _context.CartItems.Where(c => c.SessionId == sessionId && c.ApplicationUserId == null).SumAsync(c => (int?)c.Quantity) ?? 0;
                    }
                }
                return new JsonResult(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetCartCountAsync (IndexModel).");
                return new JsonResult(new { success = false, message = "Sepet sayýsý alýnamadý." });
            }
        }

        public async Task<JsonResult> OnPostAddToCartAsync(int productId, int quantity)
        {
            // ... mevcut kodunuz ...
            // Bu metodun tamamý olduðu gibi kalmalý
            if (productId <= 0 || quantity <= 0) { return new JsonResult(new { success = false, message = "Geçersiz ürün veya adet." }); }
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) { return new JsonResult(new { success = false, message = "Ürün bulunamadý." }); }
                string? currentUserId = GetCurrentUserId();
                string sessionId = GetSessionId();
                CartItem? existingItem = null;
                int currentQuantityInCart = 0;
                if (!string.IsNullOrEmpty(currentUserId)) { existingItem = await _context.CartItems.FirstOrDefaultAsync(c => c.ProductId == productId && c.ApplicationUserId == currentUserId); }
                else { existingItem = await _context.CartItems.FirstOrDefaultAsync(c => c.ProductId == productId && c.SessionId == sessionId && c.ApplicationUserId == null); }
                if (existingItem != null) { currentQuantityInCart = existingItem.Quantity; }
                if (currentQuantityInCart + quantity > product.Stock)
                {
                    int canAdd = product.Stock - currentQuantityInCart;
                    if (canAdd <= 0) { return new JsonResult(new { success = false, message = $"Stokta yeterli ürün yok." }); }
                    return new JsonResult(new { success = false, message = $"Stokta sadece {product.Stock} adet var. Sepetinize en fazla {canAdd} adet daha ekleyebilirsiniz." });
                }
                if (existingItem != null) { existingItem.Quantity += quantity; }
                else { _context.CartItems.Add(new CartItem { ProductId = productId, Quantity = quantity, ApplicationUserId = currentUserId, SessionId = sessionId }); }
                await _context.SaveChangesAsync();
                int newCartItemCount = 0;
                if (!string.IsNullOrEmpty(currentUserId)) { newCartItemCount = await _context.CartItems.Where(c => c.ApplicationUserId == currentUserId).SumAsync(c => (int?)c.Quantity) ?? 0; }
                else { newCartItemCount = await _context.CartItems.Where(c => c.SessionId == sessionId && c.ApplicationUserId == null).SumAsync(c => (int?)c.Quantity) ?? 0; }
                return new JsonResult(new { success = true, message = $"{product.Name} sepete eklendi!", newCount = newCartItemCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostAddToCartAsync for ProductId: {ProductId} (IndexModel)", productId);
                return new JsonResult(new { success = false, message = "Sepet iþlemi sýrasýnda bir hata oluþtu." });
            }
        }

        [Authorize]
        public async Task<JsonResult> OnPostToggleFavoriteAsync(int productId)
        {
            // ... mevcut kodunuz ...
            // Bu metodun tamamý olduðu gibi kalmalý
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) { return new JsonResult(new { success = false, message = "Bu iþlem için giriþ yapmalýsýnýz.", redirectToLogin = true }); }
            try
            {
                var existingFavorite = await _context.Favorites.FirstOrDefaultAsync(f => f.ApplicationUserId == userId && f.ProductId == productId);
                bool isFavoriteNow;
                if (existingFavorite != null)
                {
                    _context.Favorites.Remove(existingFavorite);
                    isFavoriteNow = false;
                }
                else
                {
                    var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
                    if (!productExists) { return new JsonResult(new { success = false, message = "Ürün bulunamadý." }); }
                    _context.Favorites.Add(new Favorite { ApplicationUserId = userId, ProductId = productId, AddedDate = DateTime.UtcNow });
                    isFavoriteNow = true;
                }
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, message = isFavoriteNow ? "Favorilere eklendi." : "Favorilerden çýkarýldý.", isFavorite = isFavoriteNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostToggleFavoriteAsync for ProductId: {ProductId}, User: {UserId} (IndexModel)", productId, userId);
                return new JsonResult(new { success = false, message = "Favori iþlemi sýrasýnda bir sunucu hatasý oluþtu." });
            }
        }
    }

    // Bu sýnýf da olduðu gibi kalýyor.
    public class CategoryTeaser
    {
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string PageUrl { get; set; } = string.Empty;
    }
}