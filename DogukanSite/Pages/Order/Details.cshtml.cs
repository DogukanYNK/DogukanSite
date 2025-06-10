using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    // Bu sayfan�n sadece giri� yapm�� kullan�c�lar taraf�ndan eri�ilmesi daha g�venli olabilir.
    // Misafir sipari� detay� i�in ayr� bir sayfa veya token bazl� bir mekanizma d���n�lebilir.
    // �imdilik [Authorize] ekliyoruz.
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
                return Forbid(); // Kullan�c� ba�kas�n�n sipari�ini g�rmeye �al���yor.
            }

            ViewData["Title"] = $"Sipari� Detay�: #{Order.Id}";
            // Sol men�de "Sipari�lerim" linkini aktif yapmak i�in
            ViewData["ActivePage"] = "OrderHistory";

            return Page();
        }
    }
}