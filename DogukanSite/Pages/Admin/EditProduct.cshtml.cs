using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations; // E�er ProductInputModel kullanacaksak
using System.Threading.Tasks;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditProductModel : PageModel
    {
        private readonly DogukanSiteContext _context;

        public EditProductModel(DogukanSiteContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product Product { get; set; }

        // E�er kategori se�imi i�in bir dropdown kullanmak isterseniz
        // public List<SelectListItem> Categories { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound("�r�n ID'si belirtilmemi�.");
            }

            Product = await _context.Products.FindAsync(id);

            if (Product == null)
            {
                return NotFound($"ID'si {id} olan �r�n bulunamad�.");
            }

            ViewData["Title"] = $"�r�n� D�zenle: {Product.Name}";

            // Kategori dropdown'� i�in (e�er AddProductModel'daki gibi kullan�lacaksa)
            // Categories = await _context.Products
            //                        .Select(p => p.Category)
            //                        .Where(c => !string.IsNullOrEmpty(c))
            //                        .Distinct()
            //                        .OrderBy(c => c)
            //                        .Select(c => new SelectListItem { Value = c, Text = c })
            //                        .ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Kategori dropdown'�n� tekrar doldurmak gerekebilir
                // OnGetAsync(Product.Id); // Tekrar �a��rmak yerine listeyi tekrar doldur
                // Categories = await _context.Products... (yukar�daki gibi)
                return Page();
            }

            _context.Attach(Product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"'{Product.Name}' �r�n� ba�ar�yla g�ncellendi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(Product.Id))
                {
                    return NotFound($"ID'si {Product.Id} olan �r�n bulunamad� (g�ncelleme s�ras�nda).");
                }
                else
                {
                    // E� zamanl�l�k hatas� i�in loglama veya �zel bir hata mesaj�
                    ModelState.AddModelError(string.Empty, "Bu �r�n ba�ka bir kullan�c� taraf�ndan g�ncellenmi� olabilir. L�tfen sayfay� yenileyip tekrar deneyin.");
                    return Page();
                }
            }

            return RedirectToPage("./ManageProducts"); // �r�n y�netimi sayfas�na geri d�n
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}