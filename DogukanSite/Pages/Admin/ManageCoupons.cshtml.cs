using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public async Task OnGetAsync()
        {
            Coupons = await _context.Coupons.OrderByDescending(c => c.Id).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (await _context.Coupons.AnyAsync(c => c.Code == NewCoupon.Code))
            {
                ModelState.AddModelError("NewCoupon.Code", "Bu kupon kodu zaten mevcut.");
            }

            if (!ModelState.IsValid)
            {
                // Hata durumunda kupon listesini tekrar yükle
                Coupons = await _context.Coupons.ToListAsync();
                return Page();
            }

            // --- YENÝ EKLENEN SATIRLAR ---
            // Tarihlerin türünü UTC olarak belirtiyoruz.
            NewCoupon.StartDate = DateTime.SpecifyKind(NewCoupon.StartDate, DateTimeKind.Utc);
            NewCoupon.EndDate = DateTime.SpecifyKind(NewCoupon.EndDate, DateTimeKind.Utc);
            // --- BÝTÝÞ ---

            // Yeni eklenen kuponun varsayýlan deðerini ayarla
            NewCoupon.UsageCount = 0;

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
    }
}