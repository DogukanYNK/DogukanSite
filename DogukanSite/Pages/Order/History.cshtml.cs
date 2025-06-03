using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // UserId almak i�in
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

        // Sipari�leri daha iyi g�stermek i�in bir ViewModel
        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal OrderTotal { get; set; }
            public string OrderStatus { get; set; }
            public int ItemCount { get; set; } // Sipari�teki toplam �r�n adedi
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Giri� yapm�� kullan�c�n�n ID'si
            if (string.IsNullOrEmpty(userId))
            {
                // Bu durum [Authorize] nedeniyle normalde olu�maz
                return Challenge(); // Veya RedirectToPage("/Account/Login");
            }

            var userOrders = await _context.Orders
                                    .Where(o => o.UserId == userId)
                                    .Include(o => o.Items) // Sipari�teki �r�n say�s�n� almak i�in
                                    .OrderByDescending(o => o.OrderDate) // En yeni sipari�ler �ste
                                    .ToListAsync();

            Orders = userOrders.Select(o => new OrderViewModel
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                OrderTotal = o.OrderTotal,
                OrderStatus = o.OrderStatus ?? "Bilinmiyor", // OrderStatus null ise varsay�lan de�er
                ItemCount = o.Items?.Sum(i => i.Quantity) ?? 0 // Items null ise veya bo�sa 0
            }).ToList();

            ViewData["Title"] = "Sipari� Ge�mi�im";
            return Page();
        }
    }
}