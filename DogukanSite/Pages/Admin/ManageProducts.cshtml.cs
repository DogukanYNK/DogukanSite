using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageProductsModel : PageModel
    {
        private readonly DogukanSiteContext _context;

        public ManageProductsModel(DogukanSiteContext context)
        {
            _context = context;
        }

        public IList<ProductViewModel> ProductsVM { get; set; } = new List<ProductViewModel>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CategoryFilter { get; set; }
        public List<SelectListItem> AllCategories { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalProductCount { get; set; }

        public class ProductViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public decimal Price { get; set; }
            public string ImageUrl { get; set; }
            public int Stock { get; set; }
        }

        public async Task OnGetAsync()
        {
            ViewData["Title"] = "Ürünleri Yönet";

            // DÜZELTME: Kategoriye göre filtreleme ve listeleme için Category verisini de sorguya dahil ediyoruz.
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(p => p.Name.Contains(SearchTerm));
            }

            if (!string.IsNullOrEmpty(CategoryFilter))
            {
                query = query.Where(p => p.Category.Name == CategoryFilter);
            }

            // DÜZELTME: Kategori listesini doðrudan Categories tablosundan, daha verimli bir þekilde çekiyoruz.
            AllCategories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Name, Text = c.Name })
                .ToListAsync();
            AllCategories.Insert(0, new SelectListItem { Value = "", Text = "Tüm Kategoriler" });

            TotalProductCount = await query.CountAsync();
            TotalPages = (int)System.Math.Ceiling(TotalProductCount / (double)PageSize);
            CurrentPage = System.Math.Max(1, System.Math.Min(CurrentPage, TotalPages == 0 ? 1 : TotalPages));

            var products = await query
                .OrderByDescending(p => p.Id)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ProductsVM = products.Select(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category?.Name ?? "Kategorisiz", // Kategori null ise "Kategorisiz" yazar.
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                Stock = p.Stock
            }).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Silinecek ürün bulunamadý.";
                return RedirectToPage(new { currentPage = CurrentPage, searchTerm = SearchTerm, categoryFilter = CategoryFilter });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{product.Name}' ürünü baþarýyla silindi.";
            return RedirectToPage(new { currentPage = CurrentPage, searchTerm = SearchTerm, categoryFilter = CategoryFilter });
        }
    }
}