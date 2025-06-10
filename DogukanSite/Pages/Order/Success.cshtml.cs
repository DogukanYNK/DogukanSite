using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    public class SuccessModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuccessModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Models.Order CurrentOrder { get; set; }
        public string CustomerEmail { get; set; }
        public int OrderId { get; set; }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            if (orderId == 0) return NotFound("Sipariþ ID bulunamadý.");

            OrderId = orderId;

            CurrentOrder = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (CurrentOrder != null)
            {
                // --- GÜVENLÝK KONTROLÜ ---
                // Eðer sipariþ bir kullanýcýya aitse, sadece o kullanýcý veya admin görebilir.
                var currentUserId = _userManager.GetUserId(User);
                if (CurrentOrder.UserId != null && CurrentOrder.UserId != currentUserId && !User.IsInRole("Admin"))
                {
                    return Forbid(); // Baþkasýnýn sipariþ onayýný görmesini engelle
                }
                // --- KONTROL SONU ---

                CustomerEmail = CurrentOrder.User?.Email ?? CurrentOrder.GuestEmail;
            }

            ViewData["Title"] = "Sipariþiniz Alýndý";
            return Page();
        }
    }
}