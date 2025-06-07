using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditCouponModel : PageModel
    {
        private readonly DogukanSiteContext _context;

        public EditCouponModel(DogukanSiteContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Coupon Coupon { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();
            Coupon = await _context.Coupons.FindAsync(id);
            if (Coupon == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Coupon.ExpiryDate.HasValue)
            {
                // Gelen tarihin türünü UTC olarak belirtiyoruz.
                Coupon.ExpiryDate = DateTime.SpecifyKind(Coupon.ExpiryDate.Value, DateTimeKind.Utc);
            }

            _context.Attach(Coupon).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{Coupon.Code}' kuponu baþarýyla güncellendi.";
            return RedirectToPage("./ManageCoupons");
        }
    }
}