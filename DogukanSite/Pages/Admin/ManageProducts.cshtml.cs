using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // SelectListItem i�in

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
        public List<SelectListItem> AllCategories { get; set; } // SelectListItem olarak de�i�tirildi

        // Sayfalama i�in
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
            // public int StockQuantity { get; set; } // E�er stok y�netimi varsa
        }

        public async Task OnGetAsync()
        {
            ViewData["Title"] = "�r�nleri Y�net";

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var searchTermLower = SearchTerm.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchTermLower));
            }

            if (!string.IsNullOrEmpty(CategoryFilter))
            {
                query = query.Where(p => p.Category == CategoryFilter);
            }

            AllCategories = await _context.Products
                                        .Select(p => p.Category)
                                        .Where(c => !string.IsNullOrEmpty(c))
                                        .Distinct()
                                        .OrderBy(c => c)
                                        .Select(c => new SelectListItem { Value = c, Text = c })
                                        .ToListAsync();
            AllCategories.Insert(0, new SelectListItem { Value = "", Text = "T�m Kategoriler" });


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
                Category = p.Category,
                Price = p.Price,
                ImageUrl = p.ImageUrl
                // StockQuantity = p.StockQuantity // E�er stok y�netimi varsa
            }).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Silinecek �r�n bulunamad�.";
                return RedirectToPage(new { currentPage = CurrentPage, searchTerm = SearchTerm, categoryFilter = CategoryFilter });
            }

            // UYARI: Bu basit silme i�lemi, ili�kili verilerde sorun yaratabilir (OrderItem, CartItem).
            // Ger�ek bir uygulamada "soft delete" (IsDeleted=true) veya ili�kili verileri
            // y�neten bir mant�k kullan�lmal�d�r.
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{product.Name}' �r�n� ba�ar�yla silindi.";
            return RedirectToPage(new { currentPage = CurrentPage, searchTerm = SearchTerm, categoryFilter = CategoryFilter });
        }
    }
}