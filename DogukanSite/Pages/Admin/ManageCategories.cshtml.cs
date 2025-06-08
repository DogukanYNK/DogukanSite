// using'ler ayný kalacak
using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // YENÝ: SelectList için eklendi
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

        // YENÝ: Üst kategori seçimi için dropdown listesi
        public SelectList CategorySelectList { get; set; }

        public async Task OnGetAsync()
        {
            // Kategorileri yüklerken, alt kategorileri ve üst kategorileri de dahil edelim (Include)
            // Bu sayede listeleme ekranýnda "Ana Kategori: Giyim" gibi bilgiler gösterebiliriz.
            Categories = await _context.Categories
                                       .Include(c => c.ParentCategory) // Üst kategoriyi getir
                                       .Include(c => c.SubCategories)  // Alt kategorileri saymak vb. için
                                       .OrderBy(c => c.ParentCategoryId) // Ana kategorileri baþta göster
                                       .ThenBy(c => c.Name)
                                       .AsNoTracking()
                                       .ToListAsync();

            // YENÝ: Dropdown'ý dolduruyoruz
            // Kendi kendisinin parent'ý olamayacaðý için listeye boþ bir "Ana Kategori" seçeneði ekliyoruz.
            await LoadCategorySelectListAsync();
        }

        // YENÝ: Kategori listesini yüklemek için yardýmcý bir metod
        private async Task LoadCategorySelectListAsync(int? selectedCategory = null)
        {
            var categoriesQuery = _context.Categories.OrderBy(c => c.Name).AsNoTracking();

            // "selectedCategory" parametresi düzenleme senaryolarý için, þimdilik null kalabilir.
            CategorySelectList = new SelectList(await categoriesQuery.ToListAsync(), "Id", "Name", selectedCategory);
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Model geçerli deðilse, sayfanýn tekrar yüklenmesi için kategori listesini doldurmamýz gerekir.
                await LoadCategorySelectListAsync();
                // Kategorilerin tam listesini de yeniden yükleyelim ki sayfa boþ görünmesin
                Categories = await _context.Categories.Include(c => c.ParentCategory).ToListAsync();
                return Page();
            }

            // DEÐÝÞTÝRÝLDÝ: NewCategory.ParentCategoryId'nin 0 gelmesi durumunu null'a çeviriyoruz.
            // HTML'deki <select> listesinde "Ana Kategori" seçeneðinin deðeri 0 veya boþ string ise,
            // bu onun bir üst kategorisi olmadýðýný gösterir. Veritabanýnda bu alaný "null" olarak kaydetmeliyiz.
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