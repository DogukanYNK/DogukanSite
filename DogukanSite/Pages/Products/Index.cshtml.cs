using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Products
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)System.Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class IndexModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ProductListViewModel ProductList { get; set; } = new();
        public List<Category> AllCategories { get; set; } = new();
        public HashSet<int> UserFavoriteProductIds { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string Category { get; set; }
        [BindProperty(SupportsGet = true)] public string SortBy { get; set; } = "newest";
        [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;

        public async Task OnGetAsync()
        {
            await LoadInitialData();
            await LoadProductsAsync();
        }

        public async Task<PartialViewResult> OnGetLoadProductsPartialAsync()
        {
            await LoadProductsAsync();
            return Partial("_ProductListPartial", ProductList);
        }

        private async Task LoadProductsAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(userId))
            {
                UserFavoriteProductIds = await _context.Favorites
                    .Where(f => f.ApplicationUserId == userId)
                    .Select(f => f.ProductId)
                    .ToHashSetAsync();
                ViewData["UserFavoriteProductIds"] = UserFavoriteProductIds;
            }

            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var searchTermLower = SearchTerm.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchTermLower));
            }

            // --- DEÐÝÞÝKLÝK BURADA BAÞLIYOR ---
            if (!string.IsNullOrEmpty(Category))
            {
                List<int> categoryIdsToFilter = await GetCategoryWithAllSubCategoryIdsAsync(Category);
                if (categoryIdsToFilter.Any())
                {
                    query = query.Where(p => categoryIdsToFilter.Contains(p.CategoryId));
                }
            }
            // --- DEÐÝÞÝKLÝK SONA ERDÝ ---

            query = SortBy?.ToLower() switch
            {
                "priceasc" => query.OrderBy(p => p.Price),
                "pricedesc" => query.OrderByDescending(p => p.Price),
                "nameasc" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.Id),
            };

            ProductList.TotalCount = await query.CountAsync();
            ProductList.PageSize = 12;
            ProductList.CurrentPage = Page;
            ProductList.Products = await query
                .Skip((Page - 1) * ProductList.PageSize)
                .Take(ProductList.PageSize)
                .ToListAsync();


        }

        private async Task LoadInitialData()
        {
            AllCategories = await _context.Categories
                .Include(c => c.SubCategories)
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        // --- YENÝ EKLENEN YARDIMCI METOT ---
        private async Task<List<int>> GetCategoryWithAllSubCategoryIdsAsync(string categoryName)
        {
            var allCategories = await _context.Categories.ToListAsync();
            var startCategory = allCategories.FirstOrDefault(c => c.Name.Equals(categoryName, System.StringComparison.OrdinalIgnoreCase));

            if (startCategory == null) return new List<int>();

            var ids = new List<int>();
            var queue = new Queue<Category>();
            queue.Enqueue(startCategory);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                ids.Add(current.Id);

                var children = allCategories.Where(c => c.ParentCategoryId == current.Id);
                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }
            return ids;
        }
    }
}