using DogukanSite.Data;
// using DogukanSite.Models; // Bu sat�r� kald�rabilir veya b�rakabilirsiniz, ama a�a��daki gibi tam ad kullanaca��z.
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

        // Order s�n�f�n� tam ad�yla (namespace dahil) belirtin
        public DogukanSite.Models.Order Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            if (orderId == null)
            {
                return NotFound("Sipari� ID'si belirtilmemi�.");
            }

            // Order s�n�f�n� tam ad�yla kullan�n
            Order = await _context.Orders
                                .Include(o => o.User)
                                .Include(o => o.Items)
                                    .ThenInclude(oi => oi.Product)
                                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (Order == null)
            {
                return NotFound($"ID'si {orderId} olan sipari� bulunamad�.");
            }
            ViewData["Title"] = $"Sipari� Detay�: #{Order.Id}";
            return Page();
        }
    }
}