// Pages/Account/Favorites.cshtml.cs
using DogukanSite.Data;
using DogukanSite.Models; // ApplicationUser için using
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Products
{
    [Authorize]
    public class FavoritesModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FavoritesModel> _logger;

        public FavoritesModel(
            DogukanSiteContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<FavoritesModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<Product> FavoriteProducts { get; set; } = new List<Product>();
        public bool HasFavorites => FavoriteProducts.Any();

        // UserFavoriteProductIds özelliði buradan kaldýrýldý.
        // Bunun yerine ViewData["IsFavoritePageContext"] kullanýlacak.

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            FavoriteProducts = await _context.Favorites
                .Where(f => f.ApplicationUserId == userId)
                .Include(f => f.Product)
                .Select(f => f.Product!)
                .ToListAsync();

            _logger.LogInformation("User {UserId} viewed their {Count} favorite products.", userId, FavoriteProducts.Count);

            // _ProductCard'a bu baðlamda olduðunu bildirmek için ViewData'yý set et
            ViewData["IsFavoritePageContext"] = true;

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveFromFavoritesOnPageAsync(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return new JsonResult(new { success = false, message = "Lütfen giriþ yapýn." });
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ApplicationUserId == userId && f.ProductId == productId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} removed product {ProductId} from favorites via Favorites page.", userId, productId);
                TempData["StatusMessage"] = "Ürün favorilerden çýkarýldý.";
                return RedirectToPage();
            }

            TempData["ErrorMessage"] = "Ürün favorilerinizde bulunamadý.";
            return RedirectToPage();
        }
    }
}