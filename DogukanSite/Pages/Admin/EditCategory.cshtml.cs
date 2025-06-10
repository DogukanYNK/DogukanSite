using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public Category Category { get; set; }

        public SelectList CategorySelectList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);

            if (Category == null) return NotFound();

            await LoadCategorySelectListAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategorySelectListAsync(Category.Id);
                return Page();
            }

            if (Category.ParentCategoryId == 0)
            {
                Category.ParentCategoryId = null;
            }

            // Bir kategorinin kendi kendisine veya kendi alt kategorisine ebeveyn olmasýný engelle
            if (Category.ParentCategoryId.HasValue)
            {
                var parent = await _context.Categories.Include(c => c.SubCategories).FirstOrDefaultAsync(c => c.Id == Category.ParentCategoryId.Value);
                // Bu kontrol daha da geliþtirilebilir, þimdilik temel bir döngü kontrolü yapýyoruz.
                if (parent != null && (parent.Id == Category.Id || IsInChildren(parent, Category.Id)))
                {
                    ModelState.AddModelError("Category.ParentCategoryId", "Bir kategori kendi alt kategorisine veya kendisine ebeveyn olarak atanamaz.");
                    await LoadCategorySelectListAsync(Category.Id);
                    return Page();
                }
            }

            _context.Attach(Category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(Category.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./ManageCategories");
        }

        private bool CategoryExists(int id) => _context.Categories.Any(e => e.Id == id);

        private async Task LoadCategorySelectListAsync(int? currentCategoryId)
        {
            var categoriesForDropdown = await _context.Categories
               .Where(c => c.Id != currentCategoryId) // Kendisini listeden çýkar
               .OrderBy(c => c.Name)
               .AsNoTracking()
               .ToListAsync();
            CategorySelectList = new SelectList(categoriesForDropdown, "Id", "Name", Category.ParentCategoryId);
        }

        // Döngüsel referansý engellemek için basit bir kontrol
        private bool IsInChildren(Category parent, int categoryIdToCheck)
        {
            if (parent.SubCategories == null) return false;
            foreach (var child in parent.SubCategories)
            {
                if (child.Id == categoryIdToCheck || IsInChildren(child, categoryIdToCheck))
                {
                    return true;
                }
            }
            return false;
        }
    }
}