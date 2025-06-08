using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // YEN�: SelectList i�in eklendi
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

        // YEN�: �st kategori se�imi i�in dropdown listesi
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

            // YEN�: Dropdown'� dolduruyoruz.
            // Kategori kendi kendisinin ebeveyni olamaz, bu y�zden listeden kendisini ��kar�yoruz.
            var categoriesForDropdown = await _context.Categories
                                                      .Where(c => c.Id != id) // Kendisini listeden ��kar
                                                      .OrderBy(c => c.Name)
                                                      .AsNoTracking()
                                                      .ToListAsync();

            // Dropdown'� haz�rlarken, veritaban�ndan gelen mevcut ParentCategoryId'yi se�ili olarak i�aretliyoruz.
            CategorySelectList = new SelectList(categoriesForDropdown, "Id", "Name", Category.ParentCategoryId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Model ge�erli de�ilse, dropdown'� tekrar doldurmam�z gerekir.
                var categoriesForDropdown = await _context.Categories
                                                          .Where(c => c.Id != Category.Id)
                                                          .OrderBy(c => c.Name)
                                                          .AsNoTracking()
                                                          .ToListAsync();
                CategorySelectList = new SelectList(categoriesForDropdown, "Id", "Name", Category.ParentCategoryId);
                return Page();
            }

            // DE���T�R�LD�: "Ana Kategori" se�ildi�inde (dropdown'dan 0 de�eri geldi�inde)
            // ParentCategoryId'yi null olarak ayarl�yoruz.
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