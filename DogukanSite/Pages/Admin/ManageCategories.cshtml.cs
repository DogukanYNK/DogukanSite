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
            [Required(ErrorMessage = "Kategori ad� bo� b�rak�lamaz.")]
            [Display(Name = "Yeni Kategori Ad�")]
            public string Name { get; set; }

            [Display(Name = "A��klama (Opsiyonel)")]
            public string Description { get; set; }
        }

        public async Task OnGetAsync()
        {
            // T�m kategorileri, i�lerindeki �r�n say�s�yla birlikte �ekiyoruz.
            Categories = await _context.Categories
                                       .Include(c => c.Products) // �li�kili �r�nleri de saymak i�in dahil et
                                       .OrderBy(c => c.Name)
                                       .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                // Hata varsa, kategori listesini tekrar y�kleyip sayfay� g�ster
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

            TempData["SuccessMessage"] = $"'{category.Name}' kategorisi ba�ar�yla eklendi.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // �NEML� KONTROL: Kategori i�inde �r�n varsa silmeyi engelle!
            if (category.Products.Any())
            {
                TempData["ErrorMessage"] = $"'{category.Name}' kategorisi i�inde �r�nler bulundu�u i�in silinemez. L�tfen �nce bu kategorideki �r�nleri ba�ka bir kategoriye ta��y�n veya silin.";
                return RedirectToPage();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{category.Name}' kategorisi ba�ar�yla silindi.";
            return RedirectToPage();
        }
    }
}