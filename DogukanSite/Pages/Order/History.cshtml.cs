using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    [Authorize]
    public class HistoryModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HistoryModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<OrderViewModel> Orders { get; set; }

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public int ItemCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var userOrders = await _context.Orders
                                           .Where(o => o.UserId == userId)
                                           .Include(o => o.OrderItems)
                                           .OrderByDescending(o => o.OrderDate)
                                           .ToListAsync();

            Orders = userOrders.Select(o => new OrderViewModel
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                ItemCount = o.OrderItems.Sum(i => i.Quantity)
            }).ToList();

            // --- DEÐÝÞÝKLÝK: ViewData atamasý buraya taþýndý ---
            ViewData["Title"] = "Sipariþ Geçmiþim";
            ViewData["ActivePage"] = "OrderHistory";

            return Page();
        }
    }
}