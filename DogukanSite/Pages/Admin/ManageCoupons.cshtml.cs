using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageCouponsModel : PageModel
    {
        private readonly DogukanSiteContext _context;

        public ManageCouponsModel(DogukanSiteContext context)
        {
            _context = context;
        }

        public IList<Coupon> Coupons { get; set; }

        [BindProperty]
        public Coupon NewCoupon { get; set; }

        public List<SelectListItem> DiscountTypes { get; set; }

        public async Task OnGetAsync()
        {
            Coupons = await _context.Coupons.OrderByDescending(c => c.Id).ToListAsync();
            PopulateDiscountTypes();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            // Ayný kodda kupon var mý kontrol et
            if (await _context.Coupons.AnyAsync(c => c.Code == NewCoupon.Code))
            {
                ModelState.AddModelError("NewCoupon.Code", "Bu kupon kodu zaten mevcut.");
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            if (NewCoupon.ExpiryDate.HasValue)
            {
                // Gelen tarihin türünü UTC olarak belirtiyoruz.
                NewCoupon.ExpiryDate = DateTime.SpecifyKind(NewCoupon.ExpiryDate.Value, DateTimeKind.Utc);
            }

            _context.Coupons.Add(NewCoupon);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{NewCoupon.Code}' kuponu baþarýyla eklendi.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{coupon.Code}' kuponu baþarýyla silindi.";
            return RedirectToPage();
        }

        private void PopulateDiscountTypes()
        {
            DiscountTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = DiscountType.Percentage.ToString(), Text = "Yüzdesel (%)" },
                new SelectListItem { Value = DiscountType.FixedAmount.ToString(), Text = "Sabit Tutar (?)" }
            };
        }
    }
}