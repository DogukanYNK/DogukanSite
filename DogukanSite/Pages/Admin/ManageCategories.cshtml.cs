using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageCategoriesModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        public ManageCategoriesModel(DogukanSiteContext context) { _context = context; }

        // --- YENÝ: Hiyerarþik yapýyý ve ürün sayýsýný bir arada tutacak ViewModel ---
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();

        [BindProperty]
        public CategoryInputModel NewCategory { get; set; }

        public SelectList CategorySelectList { get; set; }

        public class CategoryViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int ProductCount { get; set; }
            public int Level { get; set; } // Hiyerarþi seviyesini belirtmek için
        }

        public class CategoryInputModel
        {
            [Required(ErrorMessage = "Kategori adý zorunludur.")]
            public string Name { get; set; }
            public string? Description { get; set; }
            public int? ParentCategoryId { get; set; }
        }

        public async Task OnGetAsync()
        {
            var allCategories = await _context.Categories
                .Include(c => c.Products) // Ürün sayýlarýný almak için
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Kategorileri hiyerarþik olarak sýrala ve ViewModel'e dönüþtür
            Categories = GetHierarchicalCategories(allCategories.Where(c => c.ParentCategoryId == null).ToList(), 0);

            await LoadCategorySelectListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Hata durumunda sayfayý yeniden doldur
                return Page();
            }

            var category = new Category
            {
                Name = NewCategory.Name,
                Description = NewCategory.Description,
                ParentCategoryId = NewCategory.ParentCategoryId == 0 ? null : NewCategory.ParentCategoryId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // --- YENÝ: Hiyerarþik listeyi oluþturan yardýmcý metot ---
        private List<CategoryViewModel> GetHierarchicalCategories(List<Category> categories, int level)
        {
            var result = new List<CategoryViewModel>();
            foreach (var cat in categories)
            {
                result.Add(new CategoryViewModel
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    ProductCount = cat.Products.Count,
                    Level = level
                });
                // Alt kategoriler için kendini tekrar çaðýr
                if (cat.SubCategories != null && cat.SubCategories.Any())
                {
                    var orderedSubCategories = cat.SubCategories.OrderBy(sc => sc.Name).ToList();
                    result.AddRange(GetHierarchicalCategories(orderedSubCategories, level + 1));
                }
            }
            return result;
        }

        private async Task LoadCategorySelectListAsync()
        {
            var categoriesQuery = await _context.Categories.OrderBy(c => c.Name).AsNoTracking().ToListAsync();
            CategorySelectList = new SelectList(categoriesQuery, "Id", "Name");
        }
    }
}