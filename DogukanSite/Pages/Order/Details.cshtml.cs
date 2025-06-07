using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    public class DetailsModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Models.Order Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            // Sipariþi; kullanýcýsý, sipariþ kalemleri ve bu kalemlerin ürün bilgileriyle birlikte çekiyoruz.
            // Bu, en performanslý veri çekme yöntemidir.
            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category) // Kategori adýný göstermek için bunu da ekledik.
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (Order == null)
            {
                return NotFound();
            }

            // --- GÜVENLÝK DEÐÝÞÝKLÝÐÝ ---
            // Bu sipariþin mevcut kullanýcýya ait olup olmadýðýný veya kullanýcýnýn Admin olup olmadýðýný kontrol et.
            var currentUserId = _userManager.GetUserId(User);
            bool isGuestOrder = string.IsNullOrEmpty(Order.UserId);

            // Misafir sipariþleri þimdilik sadece ID ile eriþilebilir, daha sonra e-posta linki ile güvenli hale getirilebilir.
            // Eðer sipariþ bir kullanýcýya aitse ve o kullanýcý mevcut kullanýcý deðilse (ve admin de deðilse), eriþimi engelle.
            if (!isGuestOrder && Order.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                // Kullanýcý baþkasýnýn sipariþini görmeye çalýþýyor.
                return Forbid();
            }
            // --- DEÐÝÞÝKLÝK SONU ---

            ViewData["Title"] = $"Sipariþ Detayý: #{Order.Id}";
            return Page();
        }
    }
}