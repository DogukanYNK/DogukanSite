using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    // Bu sayfanýn sadece giriþ yapmýþ kullanýcýlar tarafýndan eriþilmesi daha güvenli olabilir.
    // Misafir sipariþ detayý için ayrý bir sayfa veya token bazlý bir mekanizma düþünülebilir.
    // Þimdilik [Authorize] ekliyoruz.
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class DetailsModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Models.Order Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (Order == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (Order.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid(); // Kullanýcý baþkasýnýn sipariþini görmeye çalýþýyor.
            }

            ViewData["Title"] = $"Sipariþ Detayý: #{Order.Id}";
            // Sol menüde "Sipariþlerim" linkini aktif yapmak için
            ViewData["ActivePage"] = "OrderHistory";

            return Page();
        }
    }
}