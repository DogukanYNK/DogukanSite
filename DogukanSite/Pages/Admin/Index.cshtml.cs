using DogukanSite.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using DogukanSite.Models;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingOrders { get; set; }

        public async Task OnGetAsync()
        {
            TotalProducts = await _context.Products.CountAsync();
            TotalOrders = await _context.Orders.CountAsync();
            TotalCustomers = await _userManager.Users.CountAsync();
            PendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending" || string.IsNullOrEmpty(o.OrderStatus));

            ViewData["Title"] = "Admin Yönetim Paneli";
        }
    }
}