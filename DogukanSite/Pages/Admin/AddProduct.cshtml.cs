using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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
        public ProductInputModel Input { get; set; }

        public List<SelectListItem> Categories { get; set; }

        public async Task OnGetAsync()
        {
            await PopulateCategoriesDropDownList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Form geçersizse, dropdown'ý tekrar doldur ve sayfayý hatalarla göster
                await PopulateCategoriesDropDownList();
                return Page();
            }

            var product = new Product
            {
                Name = Input.Name,
                Description = Input.Description,
                Price = Input.Price,
                DiscountPrice = Input.DiscountPrice,
                Stock = Input.Stock,
                CategoryId = Input.CategoryId,
                ImageUrl = Input.ImageUrl,
                IsFeatured = Input.IsFeatured,
                IsBestSeller = Input.IsBestSeller,
                IsNewArrival = Input.IsNewArrival,
                DateAdded = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{product.Name}' ürünü baþarýyla eklendi.";
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

        // Formdan gelen veriler için ayrý bir model kullanmak en iyi pratiktir.
        public class ProductInputModel
        {
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
        }
    }
}