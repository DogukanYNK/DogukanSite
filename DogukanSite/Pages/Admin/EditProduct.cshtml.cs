using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        public ProductInputModel Input { get; set; }

        public List<SelectListItem> Categories { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Veritabanýndan gelen Product nesnesini, formda kullanýlacak InputModel'e aktar.
            Input = new ProductInputModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Stock = product.Stock,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl,
                IsFeatured = product.IsFeatured,
                IsBestSeller = product.IsBestSeller,
                IsNewArrival = product.IsNewArrival,
                DateAdded = product.DateAdded // Eklenme tarihini koru
            };

            await PopulateCategoriesDropDownList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesDropDownList();
                return Page();
            }

            var productToUpdate = await _context.Products.FindAsync(Input.Id);
            if (productToUpdate == null) return NotFound();

            // InputModel'den gelen verilerle mevcut ürünü güncelle
            productToUpdate.Name = Input.Name;
            productToUpdate.Description = Input.Description;
            productToUpdate.Price = Input.Price;
            productToUpdate.DiscountPrice = Input.DiscountPrice;
            productToUpdate.Stock = Input.Stock;
            productToUpdate.CategoryId = Input.CategoryId;
            productToUpdate.ImageUrl = Input.ImageUrl;
            productToUpdate.IsFeatured = Input.IsFeatured;
            productToUpdate.IsBestSeller = Input.IsBestSeller;
            productToUpdate.IsNewArrival = Input.IsNewArrival;
            // DateAdded gibi alanlar güncellenmez, korunur.

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"'{productToUpdate.Name}' ürünü baþarýyla güncellendi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == Input.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./ManageProducts");
        }

        private async Task PopulateCategoriesDropDownList()
        {
            Categories = await _context.Categories
                                       .OrderBy(c => c.Name)
                                       .Select(c => new SelectListItem
                                       {
                                           Value = c.Id.ToString(),
                                           Text = c.Name
                                       }).ToListAsync();
        }

        public class ProductInputModel
        {
            public int Id { get; set; } // Güncelleme için ID gereklidir

            [Required(ErrorMessage = "Ürün adý alaný zorunludur.")]
            [Display(Name = "Ürün Adý")]
            public string Name { get; set; }

            [Display(Name = "Açýklama")]
            public string Description { get; set; }

            [Required(ErrorMessage = "Fiyat alaný zorunludur.")]
            [Range(0.01, 1000000, ErrorMessage = "Fiyat 0'dan büyük olmalýdýr.")]
            [Display(Name = "Fiyat (?)")]
            public decimal Price { get; set; }

            [Range(0.01, 1000000, ErrorMessage = "Ýndirimli Fiyat 0'dan büyük olmalýdýr.")]
            [Display(Name = "Ýndirimli Fiyat (Opsiyonel)")]
            public decimal? DiscountPrice { get; set; }

            [Required(ErrorMessage = "Stok adedi zorunludur.")]
            [Range(0, int.MaxValue, ErrorMessage = "Stok adedi negatif olamaz.")]
            [Display(Name = "Stok Adedi")]
            public int Stock { get; set; }

            [Required(ErrorMessage = "Lütfen bir kategori seçin.")]
            [Display(Name = "Kategori")]
            public int CategoryId { get; set; }

            [Url(ErrorMessage = "Lütfen geçerli bir URL giriniz.")]
            [Display(Name = "Resim URL")]
            public string ImageUrl { get; set; }

            [Display(Name = "Öne Çýkan Ürün")]
            public bool IsFeatured { get; set; }

            [Display(Name = "Çok Satan")]
            public bool IsBestSeller { get; set; }

            [Display(Name = "Yeni Ürün")]
            public bool IsNewArrival { get; set; }

            // Bu alan formda gösterilmez ama post edildiðinde kaybolmamasý için gereklidir.
            public DateTime DateAdded { get; set; }
        }
    }
}