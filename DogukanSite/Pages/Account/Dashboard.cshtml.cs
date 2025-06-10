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
        private readonly DogukanSiteContext _context; // Son sipari�leri �ekmek i�in eklendi

        public DashboardModel(
            UserManager<ApplicationUser> userManager,
            DogukanSiteContext context) // DI ile context eklendi
        {
            _userManager = userManager;
            _context = context;
        }

        // Kullan�c� bilgilerini ve son sipari�leri ta��yacak Model
        public string UserFullName { get; set; }
        public List<Models.Order> RecentOrders { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' ile bulunamad�.");
            }

            UserFullName = $"{user.FirstName} {user.LastName}".Trim();

            // Kullan�c�n�n son 3 sipari�ini veritaban�ndan �ekelim
            RecentOrders = await _context.Orders
                                         .Where(o => o.UserId == user.Id)
                                         .OrderByDescending(o => o.OrderDate)
                                         .Take(3)
                                         .ToListAsync();

            // Aktif men� ��esini do�ru �ekilde ayarlayal�m
            ViewData["ActivePage"] = "Dashboard";
            ViewData["Title"] = "Hesap Panelim";

            return Page();
        }
    }
}