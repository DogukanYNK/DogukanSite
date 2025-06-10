using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public FavoritesModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Product> FavoriteProducts { get; set; } = new List<Product>();
        public bool HasFavorites => FavoriteProducts.Any();

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            FavoriteProducts = await _context.Favorites
                .Where(f => f.ApplicationUserId == userId)
                .Include(f => f.Product).ThenInclude(p => p.Category) // Kategori bilgisi de gelsin
                .Select(f => f.Product)
                .Where(p => p != null) // Silinmiþ ürünler varsa engelle
                .ToListAsync();

            // _ProductCardPartial'ýn kalbi doðru göstermesi için favori ID'lerini set et
            var favoriteIds = new HashSet<int>(FavoriteProducts.Select(p => p.Id));
            ViewData["UserFavoriteProductIds"] = favoriteIds;

            //// Bu sayfanýn "Profil Ayarlarý" grubuna ait olduðunu belirtelim
            //ViewData["ActivePage"] = "Profile";
        }
    }
}