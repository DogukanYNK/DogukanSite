// ===== DogukanSite/Pages/Account/Dashboard.cshtml.cs =====

using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DogukanSiteContext _context; // Son sipariþleri çekmek için eklendi

        public DashboardModel(
            UserManager<ApplicationUser> userManager,
            DogukanSiteContext context) // DI ile context eklendi
        {
            _userManager = userManager;
            _context = context;
        }

        // Kullanýcý bilgilerini ve son sipariþleri taþýyacak Model
        public string UserFullName { get; set; }
        public List<Models.Order> RecentOrders { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' ile bulunamadý.");
            }

            UserFullName = $"{user.FirstName} {user.LastName}".Trim();

            // Kullanýcýnýn son 3 sipariþini veritabanýndan çekelim
            RecentOrders = await _context.Orders
                                         .Where(o => o.UserId == user.Id)
                                         .OrderByDescending(o => o.OrderDate)
                                         .Take(3)
                                         .ToListAsync();

            // Aktif menü öðesini doðru þekilde ayarlayalým
            ViewData["ActivePage"] = "Dashboard";
            ViewData["Title"] = "Hesap Panelim";

            return Page();
        }
    }
}