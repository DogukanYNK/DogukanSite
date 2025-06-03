using DogukanSite.Data;
using DogukanSite.Models; // Product modeli i�in
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks; // Asenkron Task i�in

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AddProductModel : PageModel
    {
        private readonly DogukanSiteContext _context;

        public AddProductModel(DogukanSiteContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product Product { get; set; } = new Product(); // Ba�lang��ta bo� bir Product nesnesi atayal�m

        // Kategori se�imi i�in (opsiyonel, e�er dropdown kullanacaksak)
        // public List<SelectListItem> Categories { get; set; }

        public void OnGet()
        {
            // E�er kategori dropdown'� i�in veri �ekilecekse burada yap�labilir
            // Categories = _context.Products
            //                    .Select(p => p.Category)
            //                    .Where(c => !string.IsNullOrEmpty(c))
            //                    .Distinct()
            //                    .OrderBy(c => c)
            //                    .Select(c => new SelectListItem { Value = c, Text = c })
            //                    .ToList();
            // Categories.Insert(0, new SelectListItem { Value = "", Text = "Kategori Se�in" });
        }

        public async Task<IActionResult> OnPostAsync() // Metodu asenkron yap�yoruz
        {
            if (!ModelState.IsValid)
            {
                // OnGet i�indeki kategori listesi doldurma i�lemini burada da yapmak gerekebilir
                // (E�er sayfa hata ile tekrar g�sterilecekse dropdown'�n dolu olmas� i�in)
                // OnGet(); // Basit�e OnGet'i �a��rabiliriz veya kodu tekrar edebiliriz.
                return Page();
            }

            // Yeni �r�n oldu�u i�in ID'si 0 olmal� veya atanmam�� olmal�
            Product.Id = 0;

            _context.Products.Add(Product);
            await _context.SaveChangesAsync(); // Asenkron kaydetme

            TempData["SuccessMessage"] = $"'{Product.Name}' �r�n� ba�ar�yla eklendi.";
            return RedirectToPage("/Products/Index"); // Veya admin �r�n listeleme sayfas�na
        }
    }
}