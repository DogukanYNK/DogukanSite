using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            DogukanSiteContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public HashSet<int> UserFavoriteProductIds { get; set; } = new HashSet<int>();
        public List<Product> Products { get; set; } = new List<Product>();
        public List<string> AllCategories { get; set; } = new List<string>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; } // Nullable yap�ld�

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; } // Nullable yap�ld�

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "newest"; // Varsay�lan de�er

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Bu metod sayfan�n ilk tam y�klenmesi i�in kullan�lacak
        public async Task OnGetAsync()
        {
            await LoadProductsAndCategoriesAsync();
            ViewData["UserFavoriteProductIds"] = UserFavoriteProductIds;
        }

        // Bu metod AJAX istekleri i�in kullan�lacak ve sadece partial view d�nd�recek
        public async Task<PartialViewResult> OnGetLoadProductsPartialAsync(string? searchTerm, string? category, string sortBy, int currentPage)
        {
            SearchTerm = searchTerm;
            Category = category;
            SortBy = sortBy ?? "newest";
            CurrentPage = currentPage;

            await LoadProductsAndCategoriesAsync();
            ViewData["UserFavoriteProductIds"] = UserFavoriteProductIds; // Partial view i�in de ViewData'y� set et

            // _ProductFiltersPartial i�in AllCategories de gerekebilir,
            // ama filtreler zaten bu partial i�inde oldu�u i�in tekrar y�klemeye gerek yok gibi.
            // E�er _ProductListPartial sadece Model.Products ve sayfalama bilgilerini alacaksa,
            // _ProductListPartial'�n modelini List<Product> ve sayfalama property'leri i�eren bir ViewModel yapabiliriz.
            // �imdilik t�m IndexModel'� g�nderiyoruz.
            return Partial("_ProductListPartial", this);
        }

        // �r�nleri ve kategorileri y�kleyen ortak metod
        private async Task LoadProductsAndCategoriesAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (User.Identity != null && User.Identity.IsAuthenticated && !string.IsNullOrEmpty(userId))
            {
                UserFavoriteProductIds = (await _context.Favorites
                                            .Where(f => f.ApplicationUserId == userId)
                                            .Select(f => f.ProductId)
                                            .ToListAsync())
                                            .ToHashSet();
            }

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(p => p.Name.ToLower().Contains(SearchTerm.ToLower()) ||
                                         (p.Description != null && p.Description.ToLower().Contains(SearchTerm.ToLower())));
            }
            if (!string.IsNullOrEmpty(Category))
            {
                query = query.Where(p => p.Category.Name.ToLower() == Category.ToLower());
            }

            switch (SortBy?.ToLower())
            {
                case "priceasc": query = query.OrderBy(p => p.Price).ThenBy(p => p.Id); break;
                case "pricedesc": query = query.OrderByDescending(p => p.Price).ThenBy(p => p.Id); break;
                case "nameasc": query = query.OrderBy(p => p.Name).ThenBy(p => p.Id); break;
                case "newest":
                default: query = query.OrderByDescending(p => p.Id); break;
            }

            TotalCount = await query.CountAsync();
            Products = await query
                             .Skip((CurrentPage - 1) * PageSize)
                             .Take(PageSize)
                             .ToListAsync();

            // AllCategories sadece sayfa ilk y�klendi�inde veya filtreler i�in gerekebilir.
            // AJAX ile sadece �r�n listesi g�ncelleniyorsa, AllCategories'i her seferinde �ekmeye gerek yok.
            // Ancak _ProductFiltersPartial hem sidebar'da hem offcanvas'ta kullan�ld��� i�in OnGetAsync'te kalmas� mant�kl�.
            if (AllCategories == null || !AllCategories.Any())
            {
                // Do�rudan Kategoriler tablosunu sorguluyoruz.
                // Bu y�ntem hem hatay� giderir hem de �ok daha performansl�d�r.
                AllCategories = await _context.Categories
                                              .OrderBy(c => c.Name) // Kategori ad�na g�re s�rala
                                              .Select(c => c.Name)  // Sadece kategori adlar�n� al
                                              .ToListAsync();
            }
        }
    }
}