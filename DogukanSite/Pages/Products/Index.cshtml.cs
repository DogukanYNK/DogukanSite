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
        public string? SearchTerm { get; set; } // Nullable yapýldý

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; } // Nullable yapýldý

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "newest"; // Varsayýlan deðer

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Bu metod sayfanýn ilk tam yüklenmesi için kullanýlacak
        public async Task OnGetAsync()
        {
            await LoadProductsAndCategoriesAsync();
            ViewData["UserFavoriteProductIds"] = UserFavoriteProductIds;
        }

        // Bu metod AJAX istekleri için kullanýlacak ve sadece partial view döndürecek
        public async Task<PartialViewResult> OnGetLoadProductsPartialAsync(string? searchTerm, string? category, string sortBy, int currentPage)
        {
            SearchTerm = searchTerm;
            Category = category;
            SortBy = sortBy ?? "newest";
            CurrentPage = currentPage;

            await LoadProductsAndCategoriesAsync();
            ViewData["UserFavoriteProductIds"] = UserFavoriteProductIds; // Partial view için de ViewData'yý set et

            // _ProductFiltersPartial için AllCategories de gerekebilir,
            // ama filtreler zaten bu partial içinde olduðu için tekrar yüklemeye gerek yok gibi.
            // Eðer _ProductListPartial sadece Model.Products ve sayfalama bilgilerini alacaksa,
            // _ProductListPartial'ýn modelini List<Product> ve sayfalama property'leri içeren bir ViewModel yapabiliriz.
            // Þimdilik tüm IndexModel'ý gönderiyoruz.
            return Partial("_ProductListPartial", this);
        }

        // Ürünleri ve kategorileri yükleyen ortak metod
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

            // AllCategories sadece sayfa ilk yüklendiðinde veya filtreler için gerekebilir.
            // AJAX ile sadece ürün listesi güncelleniyorsa, AllCategories'i her seferinde çekmeye gerek yok.
            // Ancak _ProductFiltersPartial hem sidebar'da hem offcanvas'ta kullanýldýðý için OnGetAsync'te kalmasý mantýklý.
            if (AllCategories == null || !AllCategories.Any())
            {
                // Doðrudan Kategoriler tablosunu sorguluyoruz.
                // Bu yöntem hem hatayý giderir hem de çok daha performanslýdýr.
                AllCategories = await _context.Categories
                                              .OrderBy(c => c.Name) // Kategori adýna göre sýrala
                                              .Select(c => c.Name)  // Sadece kategori adlarýný al
                                              .ToListAsync();
            }
        }
    }
}