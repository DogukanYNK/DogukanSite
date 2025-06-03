using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    [Authorize]
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
            if (orderId == 0) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Order = await _context.Orders
                                .Include(o => o.Items)
                                    .ThenInclude(oi => oi.Product) // Sipari� kalemlerindeki �r�nleri �ek
                                .Include(o => o.User) // Sipari�i veren kullan�c�y� �ek (opsiyonel)
                                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            // Kullan�c�n�n sadece kendi sipari�ini g�rebilmesini sa�la

            if (Order == null)
            {
                return NotFound("Sipari� bulunamad� veya bu sipari�i g�r�nt�leme yetkiniz yok.");
            }

            ViewData["Title"] = $"Sipari� Detay� #{Order.Id}";
            return Page();
        }
    }
}