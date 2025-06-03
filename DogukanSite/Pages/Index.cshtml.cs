using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Http; // IHttpContextAccessor ve Session i�in
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;     // User.FindFirstValue i�in (alternatif)
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
        public List<Product> NewArrivals { get; set; } = new List<Product>();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<CategoryTeaser> FeaturedCategories { get; set; } = new List<CategoryTeaser>();

        // === YARDIMCI METODLAR ===
        private string? GetCurrentUserId() // Nullable d�nd�rebilir
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return _userManager.GetUserId(User);
            }
            return null;
        }

        private string GetSessionId(bool createIfNull = true)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.Session == null)
            {
                _logger.LogWarning("HttpContext or Session is null in GetSessionId for IndexModel. Ensure Session middleware is configured and used.");
                if (createIfNull)
                {
                    // Acil durum i�in bir session ID �retmeye �al��abiliriz ama bu ideal de�il.
                    // Bu durumun normalde olmamas� gerekir. Session middleware'ini kontrol et.
                    var tempSessionId = Guid.NewGuid().ToString();
                    // httpContext?.Session?.SetString("SessionId", tempSessionId); // httpContext null olabilir
                    return tempSessionId;
                }
                return string.Empty;
            }

            var sessionId = httpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId) && createIfNull)
            {
                sessionId = Guid.NewGuid().ToString();
                httpContext.Session.SetString("SessionId", sessionId);
                _logger.LogInformation("New SessionId created by IndexModel: {SessionId}", sessionId);
            }
            return sessionId ?? string.Empty;
        }
        // === YARDIMCI METODLAR B�T�� ===

        public async Task OnGetAsync()
        {
            var userId = GetCurrentUserId(); // Art�k bu metod var
            if (!string.IsNullOrEmpty(userId))
            {
                UserFavoriteProductIds = (await _context.Favorites
                                            .AsNoTracking()
                                            .Where(f => f.ApplicationUserId == userId)
                                            .Select(f => f.ProductId)
                                            .ToListAsync())
                                            .ToHashSet();
            }

            NewArrivals = await _context.Products.AsNoTracking().OrderByDescending(p => p.Id).Take(4).ToListAsync();

            int totalProducts = await _context.Products.CountAsync();
            int takeCount = Math.Min(4, totalProducts);

            if (takeCount > 0)
            {
                FeaturedProducts = await _context.Products.AsNoTracking()
                    .Where(p => p.IsFeatured == true || p.Price > 100) // IsFeatured true olanlar� veya fiyat� 100'den b�y�k olanlar� al
                    .OrderBy(p => Guid.NewGuid())
                    .Take(takeCount)
                    .ToListAsync();

                if (!FeaturedProducts.Any() && totalProducts > 0) // E�er hala �ne ��kan yoksa rastgele al
                {
                    FeaturedProducts = await _context.Products.AsNoTracking()
                        .OrderBy(p => Guid.NewGuid())
                        .Take(takeCount)
                        .ToListAsync();
                }
            }
            else
            {
                FeaturedProducts = new List<Product>();
            }

            FeaturedCategories = new List<CategoryTeaser>
            {
                new CategoryTeaser { Name = "Elektronik", ImageUrl = "/images/categories/electronics.jpg", PageUrl = "/Products/Index?category=Elektronik" },
                new CategoryTeaser { Name = "Giyim", ImageUrl = "/images/categories/fashion.jpg", PageUrl = "/Products/Index?category=Giyim" },
                new CategoryTeaser { Name = "Ev & Ya�am", ImageUrl = "/images/categories/home-living.jpg", PageUrl = "/Products/Index?category=EvYasam" },
                new CategoryTeaser { Name = "Kozmetik", ImageUrl = "/images/categories/cosmetics.jpg", PageUrl = "/Products/Index?category=Kozmetik" }
            };
            ViewData["UserFavoriteProductIds"] = UserFavoriteProductIds;
            // _logger.LogInformation("Index page OnGetAsync finished.");
        }

        public async Task<JsonResult> OnGetCartCountAsync()
        {
            try
            {
                string? userId = GetCurrentUserId();
                int count = 0;
                if (!string.IsNullOrEmpty(userId))
                {
                    count = await _context.CartItems
                                     .Where(c => c.ApplicationUserId == userId)
                                     .SumAsync(c => (int?)c.Quantity) ?? 0;
                }
                else
                {
                    var sessionId = GetSessionId(createIfNull: false);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        count = await _context.CartItems
                                         .Where(c => c.SessionId == sessionId && c.ApplicationUserId == null)
                                         .SumAsync(c => (int?)c.Quantity) ?? 0;
                    }
                }
                return new JsonResult(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetCartCountAsync (IndexModel).");
                return new JsonResult(new { success = false, message = "Sepet say�s� al�namad�." });
            }
        }

        public async Task<JsonResult> OnPostAddToCartAsync(int productId, int quantity)
        {
            if (productId <= 0 || quantity <= 0)
            {
                return new JsonResult(new { success = false, message = "Ge�ersiz �r�n veya adet." });
            }
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return new JsonResult(new { success = false, message = "�r�n bulunamad�." });
                }

                string? currentUserId = GetCurrentUserId();
                string sessionId = GetSessionId(); // Yeni sepet i�in session ID olu�turulabilir/al�nabilir

                CartItem? existingItem = null;
                int currentQuantityInCart = 0;

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    existingItem = await _context.CartItems
                        .FirstOrDefaultAsync(c => c.ProductId == productId && c.ApplicationUserId == currentUserId);
                }
                else
                {
                    existingItem = await _context.CartItems
                        .FirstOrDefaultAsync(c => c.ProductId == productId && c.SessionId == sessionId && c.ApplicationUserId == null);
                }

                if (existingItem != null)
                {
                    currentQuantityInCart = existingItem.Quantity;
                }

                if (currentQuantityInCart + quantity > product.Stock)
                {
                    int canAdd = product.Stock - currentQuantityInCart;
                    if (canAdd <= 0 && product.Stock > 0)
                    { // Stok var ama sepetteki zaten max. veya fazla
                        return new JsonResult(new
                        {
                            success = false,
                            message = $"'{product.Name}' i�in sepetinizde zaten izin verilen maksimum adette ({product.Stock}) �r�n bulunuyor."
                        });
                    }
                    else if (canAdd <= 0 && product.Stock == 0)
                    { // Stok tamamen bitmi�
                        return new JsonResult(new
                        {
                            success = false,
                            message = $"'{product.Name}' i�in stokta �r�n bulunmamaktad�r."
                        });
                    }
                    // E�er eklenebilecek miktar varsa, onu bildir
                    return new JsonResult(new
                    {
                        success = false,
                        message = $"'{product.Name}' i�in stokta sadece {product.Stock} adet var. Sepetinize en fazla {canAdd} adet daha ekleyebilirsiniz."
                    });
                }

                int newCartItemCount = 0;
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        ApplicationUserId = currentUserId,
                        SessionId = sessionId
                    });
                }
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    newCartItemCount = await _context.CartItems.Where(c => c.ApplicationUserId == currentUserId).SumAsync(c => (int?)c.Quantity) ?? 0;
                }
                else
                {
                    newCartItemCount = await _context.CartItems.Where(c => c.SessionId == sessionId && c.ApplicationUserId == null).SumAsync(c => (int?)c.Quantity) ?? 0;
                }
                return new JsonResult(new { success = true, message = $"{product.Name} sepete eklendi!", newCount = newCartItemCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostAddToCartAsync for ProductId: {ProductId} (IndexModel)", productId);
                return new JsonResult(new { success = false, message = "Sepet i�lemi s�ras�nda bir hata olu�tu." });
            }
        }

        [Authorize]
        public async Task<JsonResult> OnPostToggleFavoriteAsync(int productId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return new JsonResult(new { success = false, message = "Bu i�lem i�in giri� yapmal�s�n�z.", redirectToLogin = true });
            }
            try
            {
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.ApplicationUserId == userId && f.ProductId == productId);
                bool isFavoriteNow;
                if (existingFavorite != null)
                {
                    _context.Favorites.Remove(existingFavorite);
                    isFavoriteNow = false;
                }
                else
                {
                    var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
                    if (!productExists)
                    {
                        return new JsonResult(new { success = false, message = "�r�n bulunamad�." });
                    }
                    _context.Favorites.Add(new Favorite
                    {
                        ApplicationUserId = userId,
                        ProductId = productId,
                        AddedDate = DateTime.UtcNow
                    });
                    isFavoriteNow = true;
                }
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, message = isFavoriteNow ? "Favorilere eklendi." : "Favorilerden ��kar�ld�.", isFavorite = isFavoriteNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostToggleFavoriteAsync for ProductId: {ProductId}, User: {UserId} (IndexModel)", productId, userId);
                return new JsonResult(new { success = false, message = "Favori i�lemi s�ras�nda bir sunucu hatas� olu�tu." });
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