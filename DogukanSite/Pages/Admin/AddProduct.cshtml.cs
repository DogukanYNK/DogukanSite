using DogukanSite.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AddProductModel : PageModel
    {
        private readonly ECommerceDbContext _context;

        public AddProductModel(ECommerceDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.Product Product { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Products.Add(Product);
            _context.SaveChanges();
            return RedirectToPage("/Products/Index");
        }
    }

}
