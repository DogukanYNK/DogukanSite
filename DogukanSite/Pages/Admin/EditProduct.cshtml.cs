using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations; // Eðer ProductInputModel kullanacaksak
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

        // Eðer kategori seçimi için bir dropdown kullanmak isterseniz
        // public List<SelectListItem> Categories { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound("Ürün ID'si belirtilmemiþ.");
            }

            Product = await _context.Products.FindAsync(id);

            if (Product == null)
            {
                return NotFound($"ID'si {id} olan ürün bulunamadý.");
            }

            ViewData["Title"] = $"Ürünü Düzenle: {Product.Name}";

            // Kategori dropdown'ý için (eðer AddProductModel'daki gibi kullanýlacaksa)
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
                // Kategori dropdown'ýný tekrar doldurmak gerekebilir
                // OnGetAsync(Product.Id); // Tekrar çaðýrmak yerine listeyi tekrar doldur
                // Categories = await _context.Products... (yukarýdaki gibi)
                return Page();
            }

            _context.Attach(Product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"'{Product.Name}' ürünü baþarýyla güncellendi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(Product.Id))
                {
                    return NotFound($"ID'si {Product.Id} olan ürün bulunamadý (güncelleme sýrasýnda).");
                }
                else
                {
                    // Eþ zamanlýlýk hatasý için loglama veya özel bir hata mesajý
                    ModelState.AddModelError(string.Empty, "Bu ürün baþka bir kullanýcý tarafýndan güncellenmiþ olabilir. Lütfen sayfayý yenileyip tekrar deneyin.");
                    return Page();
                }
            }

            return RedirectToPage("./ManageProducts"); // Ürün yönetimi sayfasýna geri dön
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}