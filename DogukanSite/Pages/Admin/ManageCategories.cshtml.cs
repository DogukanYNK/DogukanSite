using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public ManageCategoriesModel(DogukanSiteContext context)
        {
            _context = context;
        }

        public IList<Category> Categories { get; set; }

        [BindProperty]
        public NewCategoryInputModel NewCategory { get; set; }

        public class NewCategoryInputModel
        {
            [Required(ErrorMessage = "Kategori adý boþ býrakýlamaz.")]
            [Display(Name = "Yeni Kategori Adý")]
            public string Name { get; set; }

            [Display(Name = "Açýklama (Opsiyonel)")]
            public string Description { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Tüm kategorileri, içlerindeki ürün sayýsýyla birlikte çekiyoruz.
            Categories = await _context.Categories
                                       .Include(c => c.Products) // Ýliþkili ürünleri de saymak için dahil et
                                       .OrderBy(c => c.Name)
                                       .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                // Hata varsa, kategori listesini tekrar yükleyip sayfayý göster
                await OnGetAsync();
                return Page();
            }

            var category = new Category
            {
                Name = NewCategory.Name,
                Description = NewCategory.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{category.Name}' kategorisi baþarýyla eklendi.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // ÖNEMLÝ KONTROL: Kategori içinde ürün varsa silmeyi engelle!
            if (category.Products.Any())
            {
                TempData["ErrorMessage"] = $"'{category.Name}' kategorisi içinde ürünler bulunduðu için silinemez. Lütfen önce bu kategorideki ürünleri baþka bir kategoriye taþýyýn veya silin.";
                return RedirectToPage();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{category.Name}' kategorisi baþarýyla silindi.";
            return RedirectToPage();
        }
    }
}