using DogukanSite.Data; // ECommerceDbContext i�in
using DogukanSite.Models; // Order i�in
using Microsoft.AspNetCore.Identity; // UserManager i�in (e�er kullan�c� bilgilerini �ekeceksek)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // FirstOrDefaultAsync i�in
using System.Threading.Tasks; // Asenkron Task i�in

namespace DogukanSite.Pages.Order
{
    public class SuccessModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Opsiyonel

        public SuccessModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int OrderId { get; set; }
        public Models.Order CurrentOrder { get; set; } // Sipari� detaylar�n� g�stermek i�in
        public string CustomerEmail { get; set; } // E-posta onay� i�in

        public async Task<IActionResult> OnGetAsync(int orderId) // Parametre ad�n� orderId olarak de�i�tirdim
        {
            if (orderId == 0)
            {
                return NotFound("Sipari� ID bulunamad�.");
            }

            OrderId = orderId;
            CurrentOrder = await _context.Orders
                                    // .Include(o => o.User) // E�er kullan�c� bilgilerini de g�stermek isterseniz
                                    .FirstOrDefaultAsync(o => o.Id == orderId);

            if (CurrentOrder == null)
            {
                return NotFound($"Sipari� ID {orderId} ile e�le�en bir sipari� bulunamad�.");
            }

            // E-posta adresini alal�m (e�er sipari�te veya kullan�c�da varsa)
            if (!string.IsNullOrEmpty(CurrentOrder.Email))
            {
                CustomerEmail = CurrentOrder.Email;
            }
            // else if (CurrentOrder.User != null && !string.IsNullOrEmpty(CurrentOrder.User.Email))
            // {
            //     CustomerEmail = CurrentOrder.User.Email;
            // }
            // else if (User.Identity.IsAuthenticated) // O anki giri� yapm�� kullan�c�dan
            // {
            //     var user = await _userManager.GetUserAsync(User);
            //     CustomerEmail = user?.Email;
            // }


            // Sipari� al�nd�ktan sonra sepeti temizleme i�lemi burada da yap�labilir
            // (e�er CreateModel'da yap�lmad�ysa veya ek g�vence olarak)
            // var sessionId = HttpContext.Session.GetString("SessionId");
            // if (!string.IsNullOrEmpty(sessionId))
            // {
            //     var cartItems = await _context.CartItems
            //                                 .Where(c => c.SessionId == sessionId)
            //                                 .ToListAsync();
            //     if (cartItems.Any())
            //     {
            //         _context.CartItems.RemoveRange(cartItems);
            //         await _context.SaveChangesAsync();
            //         HttpContext.Session.Remove("SessionId"); // Session'� da temizleyebiliriz
            //     }
            // }

            ViewData["Title"] = "Sipari�iniz Al�nd�";
            return Page();
        }
    }
}