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
using System.Security.Claims;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    [Authorize] // Sadece giriþ yapmýþ kullanýcýlar eriþebilir
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

        // Sipariþleri arayüzde daha iyi göstermek için bir ViewModel
        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; } // Düzeltildi: OrderTotal -> TotalAmount
            public string Status { get; set; }      // Düzeltildi: OrderStatus -> Status
            public int ItemCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User); // Bu yöntem daha güvenilirdir.
            if (string.IsNullOrEmpty(userId))
            {
                // [Authorize] etiketi sayesinde bu normalde gerçekleþmez.
                return Challenge();
            }

            // Düzeltme: .Include(o => o.Items) -> .Include(o => o.OrderItems)
            var userOrders = await _context.Orders
                                           .Where(o => o.UserId == userId)
                                           .Include(o => o.OrderItems) // Sipariþteki ürün sayýsýný almak için
                                           .OrderByDescending(o => o.OrderDate) // En yeni sipariþler üste
                                           .ToListAsync();

            // Düzeltme: ViewModel'e atama yaparken yeni modeldeki doðru alan adlarý kullanýldý.
            Orders = userOrders.Select(o => new OrderViewModel
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,         // OrderTotal -> TotalAmount
                Status = o.Status.ToString(),        // OrderStatus (string) -> Status (enum), metne çevrildi
                ItemCount = o.OrderItems.Sum(i => i.Quantity) // Items -> OrderItems
            }).ToList();

            ViewData["Title"] = "Sipariþ Geçmiþim";
            return Page();
        }
    }
}