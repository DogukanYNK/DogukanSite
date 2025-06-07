using DogukanSite.Data;
using DogukanSite.Models; // Order ve ApplicationUser i�in gerekli
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class OrderDetailsModel : PageModel
    {
        private readonly DogukanSiteContext _veritabani;

        public OrderDetailsModel(DogukanSiteContext context)
        {
            _veritabani = context;
        }

        // Razor sayfas�nda kullanmak i�in Sipari� nesnesi
        public DogukanSite.Models.Order Siparis { get; set; }

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            if (orderId == null)
            {
                return NotFound("Sipari� ID'si belirtilmemi�.");
            }

            // D�zeltme: Sorguya ili�kili verileri de dahil ediyoruz.
            Siparis = await _veritabani.Orders
                .Include(o => o.User) // Sipari�i veren kullan�c�y� al
                .Include(o => o.OrderItems) // Sipari� kalemlerini al
                    .ThenInclude(oi => oi.Product) // Kalemlere ait �r�nleri al
                        .ThenInclude(p => p.Category) // �r�nlere ait kategorileri al
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (Siparis == null)
            {
                return NotFound($"ID'si {orderId} olan sipari� bulunamad�.");
            }

            ViewData["Title"] = $"Sipari� Detay�: #{Siparis.Id}";
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, OrderStatus newStatus)
        {
            var siparis = await _veritabani.Orders.FindAsync(orderId);
            if (siparis == null)
            {
                return NotFound();
            }

            siparis.Status = newStatus;
            await _veritabani.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Sipari� #{orderId} durumu '{newStatus}' olarak g�ncellendi.";
            // Kullan�c�y� detay sayfas�nda tutmak yerine, filtrelerin kaybolmamas� i�in ana listeye y�nlendirmek daha iyi bir UX olabilir.
            return RedirectToPage("./ManageOrders");
        }
    }
}