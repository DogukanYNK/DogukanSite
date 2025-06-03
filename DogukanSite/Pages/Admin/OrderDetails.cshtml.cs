using DogukanSite.Data;
// using DogukanSite.Models; // Bu satýrý kaldýrabilir veya býrakabilirsiniz, ama aþaðýdaki gibi tam ad kullanacaðýz.
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
        private readonly DogukanSiteContext _context;

        public OrderDetailsModel(DogukanSiteContext context)
        {
            _context = context;
        }

        // Order sýnýfýný tam adýyla (namespace dahil) belirtin
        public DogukanSite.Models.Order Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            if (orderId == null)
            {
                return NotFound("Sipariþ ID'si belirtilmemiþ.");
            }

            // Order sýnýfýný tam adýyla kullanýn
            Order = await _context.Orders
                                .Include(o => o.User)
                                .Include(o => o.Items)
                                    .ThenInclude(oi => oi.Product)
                                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (Order == null)
            {
                return NotFound($"ID'si {orderId} olan sipariþ bulunamadý.");
            }
            ViewData["Title"] = $"Sipariþ Detayý: #{Order.Id}";
            return Page();
        }
    }
}