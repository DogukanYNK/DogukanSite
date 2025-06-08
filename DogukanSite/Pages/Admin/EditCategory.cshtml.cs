using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // YENÝ: SelectList için eklendi
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditCategoryModel : PageModel
    {
        private readonly DogukanSiteContext _context;

        public EditCategoryModel(DogukanSiteContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Category Category { get; set; } = default!;

        // YENÝ: Üst kategori seçimi için dropdown listesi
        public SelectList CategorySelectList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);

            if (Category == null)
            {
                return NotFound();
            }

            // YENÝ: Dropdown'ý dolduruyoruz.
            // Kategori kendi kendisinin ebeveyni olamaz, bu yüzden listeden kendisini çýkarýyoruz.
            var categoriesForDropdown = await _context.Categories
                                                      .Where(c => c.Id != id) // Kendisini listeden çýkar
                                                      .OrderBy(c => c.Name)
                                                      .AsNoTracking()
                                                      .ToListAsync();

            // Dropdown'ý hazýrlarken, veritabanýndan gelen mevcut ParentCategoryId'yi seçili olarak iþaretliyoruz.
            CategorySelectList = new SelectList(categoriesForDropdown, "Id", "Name", Category.ParentCategoryId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Model geçerli deðilse, dropdown'ý tekrar doldurmamýz gerekir.
                var categoriesForDropdown = await _context.Categories
                                                          .Where(c => c.Id != Category.Id)
                                                          .OrderBy(c => c.Name)
                                                          .AsNoTracking()
                                                          .ToListAsync();
                CategorySelectList = new SelectList(categoriesForDropdown, "Id", "Name", Category.ParentCategoryId);
                return Page();
            }

            // DEÐÝÞTÝRÝLDÝ: "Ana Kategori" seçildiðinde (dropdown'dan 0 deðeri geldiðinde)
            // ParentCategoryId'yi null olarak ayarlýyoruz.
            if (Category.ParentCategoryId == 0)
            {
                Category.ParentCategoryId = null;
            }

            _context.Attach(Category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(Category.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./ManageCategories");
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}