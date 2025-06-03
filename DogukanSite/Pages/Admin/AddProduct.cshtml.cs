using DogukanSite.Data;
using DogukanSite.Models; // Product modeli için
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks; // Asenkron Task için

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
        public Product Product { get; set; } = new Product(); // Baþlangýçta boþ bir Product nesnesi atayalým

        // Kategori seçimi için (opsiyonel, eðer dropdown kullanacaksak)
        // public List<SelectListItem> Categories { get; set; }

        public void OnGet()
        {
            // Eðer kategori dropdown'ý için veri çekilecekse burada yapýlabilir
            // Categories = _context.Products
            //                    .Select(p => p.Category)
            //                    .Where(c => !string.IsNullOrEmpty(c))
            //                    .Distinct()
            //                    .OrderBy(c => c)
            //                    .Select(c => new SelectListItem { Value = c, Text = c })
            //                    .ToList();
            // Categories.Insert(0, new SelectListItem { Value = "", Text = "Kategori Seçin" });
        }

        public async Task<IActionResult> OnPostAsync() // Metodu asenkron yapýyoruz
        {
            if (!ModelState.IsValid)
            {
                // OnGet içindeki kategori listesi doldurma iþlemini burada da yapmak gerekebilir
                // (Eðer sayfa hata ile tekrar gösterilecekse dropdown'ýn dolu olmasý için)
                // OnGet(); // Basitçe OnGet'i çaðýrabiliriz veya kodu tekrar edebiliriz.
                return Page();
            }

            // Yeni ürün olduðu için ID'si 0 olmalý veya atanmamýþ olmalý
            Product.Id = 0;

            _context.Products.Add(Product);
            await _context.SaveChangesAsync(); // Asenkron kaydetme

            TempData["SuccessMessage"] = $"'{Product.Name}' ürünü baþarýyla eklendi.";
            return RedirectToPage("/Products/Index"); // Veya admin ürün listeleme sayfasýna
        }
    }
}