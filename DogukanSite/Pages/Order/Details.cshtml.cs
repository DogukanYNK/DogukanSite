using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
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
            // Sipari�i; kullan�c�s�, sipari� kalemleri ve bu kalemlerin �r�n bilgileriyle birlikte �ekiyoruz.
            // Bu, en performansl� veri �ekme y�ntemidir.
            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category) // Kategori ad�n� g�stermek i�in bunu da ekledik.
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (Order == null)
            {
                return NotFound();
            }

            // --- G�VENL�K DE����KL��� ---
            // Bu sipari�in mevcut kullan�c�ya ait olup olmad���n� veya kullan�c�n�n Admin olup olmad���n� kontrol et.
            var currentUserId = _userManager.GetUserId(User);
            bool isGuestOrder = string.IsNullOrEmpty(Order.UserId);

            // Misafir sipari�leri �imdilik sadece ID ile eri�ilebilir, daha sonra e-posta linki ile g�venli hale getirilebilir.
            // E�er sipari� bir kullan�c�ya aitse ve o kullan�c� mevcut kullan�c� de�ilse (ve admin de de�ilse), eri�imi engelle.
            if (!isGuestOrder && Order.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                // Kullan�c� ba�kas�n�n sipari�ini g�rmeye �al���yor.
                return Forbid();
            }
            // --- DE����KL�K SONU ---

            ViewData["Title"] = $"Sipari� Detay�: #{Order.Id}";
            return Page();
        }
    }
}