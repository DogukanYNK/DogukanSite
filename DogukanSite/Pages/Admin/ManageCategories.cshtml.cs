// using'ler ayn� kalacak
using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // YEN�: SelectList i�in eklendi
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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

        public IList<Category> Categories { get; set; } = new List<Category>();

        [BindProperty]
        public Category NewCategory { get; set; } = default!;

        // YEN�: �st kategori se�imi i�in dropdown listesi
        public SelectList CategorySelectList { get; set; }

        public async Task OnGetAsync()
        {
            // Kategorileri y�klerken, alt kategorileri ve �st kategorileri de dahil edelim (Include)
            // Bu sayede listeleme ekran�nda "Ana Kategori: Giyim" gibi bilgiler g�sterebiliriz.
            Categories = await _context.Categories
                                       .Include(c => c.ParentCategory) // �st kategoriyi getir
                                       .Include(c => c.SubCategories)  // Alt kategorileri saymak vb. i�in
                                       .OrderBy(c => c.ParentCategoryId) // Ana kategorileri ba�ta g�ster
                                       .ThenBy(c => c.Name)
                                       .AsNoTracking()
                                       .ToListAsync();

            // YEN�: Dropdown'� dolduruyoruz
            // Kendi kendisinin parent'� olamayaca�� i�in listeye bo� bir "Ana Kategori" se�ene�i ekliyoruz.
            await LoadCategorySelectListAsync();
        }

        // YEN�: Kategori listesini y�klemek i�in yard�mc� bir metod
        private async Task LoadCategorySelectListAsync(int? selectedCategory = null)
        {
            var categoriesQuery = _context.Categories.OrderBy(c => c.Name).AsNoTracking();

            // "selectedCategory" parametresi d�zenleme senaryolar� i�in, �imdilik null kalabilir.
            CategorySelectList = new SelectList(await categoriesQuery.ToListAsync(), "Id", "Name", selectedCategory);
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Model ge�erli de�ilse, sayfan�n tekrar y�klenmesi i�in kategori listesini doldurmam�z gerekir.
                await LoadCategorySelectListAsync();
                // Kategorilerin tam listesini de yeniden y�kleyelim ki sayfa bo� g�r�nmesin
                Categories = await _context.Categories.Include(c => c.ParentCategory).ToListAsync();
                return Page();
            }

            // DE���T�R�LD�: NewCategory.ParentCategoryId'nin 0 gelmesi durumunu null'a �eviriyoruz.
            // HTML'deki <select> listesinde "Ana Kategori" se�ene�inin de�eri 0 veya bo� string ise,
            // bu onun bir �st kategorisi olmad���n� g�sterir. Veritaban�nda bu alan� "null" olarak kaydetmeliyiz.
            if (NewCategory.ParentCategoryId == 0)
            {
                NewCategory.ParentCategoryId = null;
            }

            _context.Categories.Add(NewCategory);
            await _context.SaveChangesAsync();

            return RedirectToPage("./ManageCategories");
        }
    }
}