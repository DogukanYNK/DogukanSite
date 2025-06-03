using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // UserId almak için
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

        // Sipariþleri daha iyi göstermek için bir ViewModel
        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal OrderTotal { get; set; }
            public string OrderStatus { get; set; }
            public int ItemCount { get; set; } // Sipariþteki toplam ürün adedi
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Giriþ yapmýþ kullanýcýnýn ID'si
            if (string.IsNullOrEmpty(userId))
            {
                // Bu durum [Authorize] nedeniyle normalde oluþmaz
                return Challenge(); // Veya RedirectToPage("/Account/Login");
            }

            var userOrders = await _context.Orders
                                    .Where(o => o.UserId == userId)
                                    .Include(o => o.Items) // Sipariþteki ürün sayýsýný almak için
                                    .OrderByDescending(o => o.OrderDate) // En yeni sipariþler üste
                                    .ToListAsync();

            Orders = userOrders.Select(o => new OrderViewModel
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                OrderTotal = o.OrderTotal,
                OrderStatus = o.OrderStatus ?? "Bilinmiyor", // OrderStatus null ise varsayýlan deðer
                ItemCount = o.Items?.Sum(i => i.Quantity) ?? 0 // Items null ise veya boþsa 0
            }).ToList();

            ViewData["Title"] = "Sipariþ Geçmiþim";
            return Page();
        }
    }
}