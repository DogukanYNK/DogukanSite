using DogukanSite.Data;
using DogukanSite.Models; // Order ve ApplicationUser için gerekli
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

        // Razor sayfasýnda kullanmak için Sipariþ nesnesi
        public DogukanSite.Models.Order Siparis { get; set; }

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            if (orderId == null)
            {
                return NotFound("Sipariþ ID'si belirtilmemiþ.");
            }

            // Düzeltme: Sorguya iliþkili verileri de dahil ediyoruz.
            Siparis = await _veritabani.Orders
                .Include(o => o.User) // Sipariþi veren kullanýcýyý al
                .Include(o => o.OrderItems) // Sipariþ kalemlerini al
                    .ThenInclude(oi => oi.Product) // Kalemlere ait ürünleri al
                        .ThenInclude(p => p.Category) // Ürünlere ait kategorileri al
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (Siparis == null)
            {
                return NotFound($"ID'si {orderId} olan sipariþ bulunamadý.");
            }

            ViewData["Title"] = $"Sipariþ Detayý: #{Siparis.Id}";
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

            TempData["SuccessMessage"] = $"Sipariþ #{orderId} durumu '{newStatus}' olarak güncellendi.";
            // Kullanýcýyý detay sayfasýnda tutmak yerine, filtrelerin kaybolmamasý için ana listeye yönlendirmek daha iyi bir UX olabilir.
            return RedirectToPage("./ManageOrders");
        }
    }
}