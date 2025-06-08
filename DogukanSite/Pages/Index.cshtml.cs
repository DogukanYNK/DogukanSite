using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages
{
    // ValidateAntiForgeryToken'ý tüm POST iþlemleri için koruma olarak eklemek iyi bir pratiktir.
    [ValidateAntiForgeryToken]
    public class IndexModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager, ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<Product> NewArrivals { get; set; } = new List<Product>();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<CategoryTeaser> FeaturedCategories { get; set; } = new List<CategoryTeaser>();
        public HashSet<int> UserFavoriteProductIds { get; set; } = new HashSet<int>();

        private string GetCurrentUserId()
        {
            return _userManager.GetUserId(User);
        }

        private string GetSessionId(bool createIfNull = true)
        {
            string sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId) && createIfNull)
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
            }
            return sessionId ?? string.Empty;
        }

        public async Task OnGetAsync()
        {
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                UserFavoriteProductIds = (await _context.Favorites
                    .AsNoTracking()
                    .Where(f => f.ApplicationUserId == userId) // DÜZELTÝLDÝ
                    .Select(f => f.ProductId)
                    .ToListAsync())
                    .ToHashSet();
            }

            NewArrivals = await _context.Products
                .Where(p => p.IsNewArrival)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            FeaturedProducts = await _context.Products
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.Id)
                .Take(4)
                .AsNoTracking()
                .ToListAsync();

            // Bu kýsým sabit olduðu için korunabilir.
            FeaturedCategories = new List<CategoryTeaser>
            {
                new CategoryTeaser { Name = "Elektronik", ImageUrl = "/images/categories/electronics.jpg", PageUrl = "/Products/Index?category=Elektronik" },
                new CategoryTeaser { Name = "Giyim", ImageUrl = "/images/categories/fashion.jpg", PageUrl = "/Products/Index?category=Giyim" },
                new CategoryTeaser { Name = "Ev & Yaþam", ImageUrl = "/images/categories/home-living.jpg", PageUrl = "/Products/Index?category=EvYasam" },
                new CategoryTeaser { Name = "Kozmetik", ImageUrl = "/images/categories/cosmetics.jpg", PageUrl = "/Products/Index?category=Kozmetik" }
            };
        }

        public async Task<JsonResult> OnGetCartCountAsync()
        {
            try
            {
                string userId = GetCurrentUserId();
                int count = 0;
                if (!string.IsNullOrEmpty(userId))
                {
                    count = await _context.CartItems.Where(c => c.ApplicationUserId == userId).SumAsync(c => (int?)c.Quantity) ?? 0; // DÜZELTÝLDÝ
                }
                else
                {
                    var sessionId = GetSessionId(createIfNull: false);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        count = await _context.CartItems.Where(c => c.SessionId == sessionId && c.ApplicationUserId == null).SumAsync(c => (int?)c.Quantity) ?? 0; // DÜZELTÝLDÝ
                    }
                }
                return new JsonResult(new { success = true, count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetCartCountAsync (IndexModel).");
                return new JsonResult(new { success = false, message = "Sepet sayýsý alýnamadý." });
            }
        }

        public async Task<JsonResult> OnPostAddToCartAsync(int productId, int quantity)
        {
            if (productId <= 0 || quantity <= 0) return new JsonResult(new { success = false, message = "Geçersiz ürün veya adet." });

            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return new JsonResult(new { success = false, message = "Ürün bulunamadý." });
                if (product.Stock < quantity) return new JsonResult(new { success = false, message = $"Stokta yeterli ürün yok. Sadece {product.Stock} adet mevcut." });

                string currentUserId = GetCurrentUserId();
                string sessionId = string.IsNullOrEmpty(currentUserId) ? GetSessionId() : null;

                CartItem existingItem = null;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    existingItem = await _context.CartItems.FirstOrDefaultAsync(c => c.ProductId == productId && c.ApplicationUserId == currentUserId); // DÜZELTÝLDÝ
                }
                else
                {
                    existingItem = await _context.CartItems.FirstOrDefaultAsync(c => c.ProductId == productId && c.SessionId == sessionId && c.ApplicationUserId == null); // DÜZELTÝLDÝ
                }

                if (existingItem != null)
                {
                    if (existingItem.Quantity + quantity > product.Stock)
                        return new JsonResult(new { success = false, message = "Bu ürün için stok limitine ulaþtýnýz." });

                    existingItem.Quantity += quantity;
                }
                else
                {
                    _context.CartItems.Add(new CartItem { ProductId = productId, Quantity = quantity, ApplicationUserId = currentUserId, SessionId = sessionId }); // DÜZELTÝLDÝ
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, message = $"{product.Name} sepete eklendi!" });
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
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return new JsonResult(new { success = false, message = "Bu iþlem için giriþ yapmalýsýnýz.", redirectToLogin = true });

            try
            {
                var existingFavorite = await _context.Favorites.FirstOrDefaultAsync(f => f.ApplicationUserId == userId && f.ProductId == productId); // DÜZELTÝLDÝ
                bool isFavoriteNow;

                if (existingFavorite != null)
                {
                    _context.Favorites.Remove(existingFavorite);
                    isFavoriteNow = false;
                }
                else
                {
                    _context.Favorites.Add(new Favorite { ApplicationUserId = userId, ProductId = productId }); // DÜZELTÝLDÝ
                    isFavoriteNow = true;
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, isFavorite = isFavoriteNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostToggleFavoriteAsync for ProductId: {ProductId}, User: {UserId} (IndexModel)", productId, userId);
                return new JsonResult(new { success = false, message = "Favori iþlemi sýrasýnda bir sunucu hatasý oluþtu." });
            }
        }
    }

    public class CategoryTeaser
    {
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string PageUrl { get; set; } = string.Empty;
    }
}