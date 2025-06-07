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
    [Authorize] // Sadece giri� yapm�� kullan�c�lar eri�ebilir
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

        // Sipari�leri aray�zde daha iyi g�stermek i�in bir ViewModel
        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; } // D�zeltildi: OrderTotal -> TotalAmount
            public string Status { get; set; }      // D�zeltildi: OrderStatus -> Status
            public int ItemCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User); // Bu y�ntem daha g�venilirdir.
            if (string.IsNullOrEmpty(userId))
            {
                // [Authorize] etiketi sayesinde bu normalde ger�ekle�mez.
                return Challenge();
            }

            // D�zeltme: .Include(o => o.Items) -> .Include(o => o.OrderItems)
            var userOrders = await _context.Orders
                                           .Where(o => o.UserId == userId)
                                           .Include(o => o.OrderItems) // Sipari�teki �r�n say�s�n� almak i�in
                                           .OrderByDescending(o => o.OrderDate) // En yeni sipari�ler �ste
                                           .ToListAsync();

            // D�zeltme: ViewModel'e atama yaparken yeni modeldeki do�ru alan adlar� kullan�ld�.
            Orders = userOrders.Select(o => new OrderViewModel
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,         // OrderTotal -> TotalAmount
                Status = o.Status.ToString(),        // OrderStatus (string) -> Status (enum), metne �evrildi
                ItemCount = o.OrderItems.Sum(i => i.Quantity) // Items -> OrderItems
            }).ToList();

            ViewData["Title"] = "Sipari� Ge�mi�im";
            return Page();
        }
    }
}