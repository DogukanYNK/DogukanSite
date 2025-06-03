using DogukanSite.Data; // ECommerceDbContext için
using DogukanSite.Models; // Order için
using Microsoft.AspNetCore.Identity; // UserManager için (eðer kullanýcý bilgilerini çekeceksek)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // FirstOrDefaultAsync için
using System.Threading.Tasks; // Asenkron Task için

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
        public Models.Order CurrentOrder { get; set; } // Sipariþ detaylarýný göstermek için
        public string CustomerEmail { get; set; } // E-posta onayý için

        public async Task<IActionResult> OnGetAsync(int orderId) // Parametre adýný orderId olarak deðiþtirdim
        {
            if (orderId == 0)
            {
                return NotFound("Sipariþ ID bulunamadý.");
            }

            OrderId = orderId;
            CurrentOrder = await _context.Orders
                                    // .Include(o => o.User) // Eðer kullanýcý bilgilerini de göstermek isterseniz
                                    .FirstOrDefaultAsync(o => o.Id == orderId);

            if (CurrentOrder == null)
            {
                return NotFound($"Sipariþ ID {orderId} ile eþleþen bir sipariþ bulunamadý.");
            }

            // E-posta adresini alalým (eðer sipariþte veya kullanýcýda varsa)
            if (!string.IsNullOrEmpty(CurrentOrder.Email))
            {
                CustomerEmail = CurrentOrder.Email;
            }
            // else if (CurrentOrder.User != null && !string.IsNullOrEmpty(CurrentOrder.User.Email))
            // {
            //     CustomerEmail = CurrentOrder.User.Email;
            // }
            // else if (User.Identity.IsAuthenticated) // O anki giriþ yapmýþ kullanýcýdan
            // {
            //     var user = await _userManager.GetUserAsync(User);
            //     CustomerEmail = user?.Email;
            // }


            // Sipariþ alýndýktan sonra sepeti temizleme iþlemi burada da yapýlabilir
            // (eðer CreateModel'da yapýlmadýysa veya ek güvence olarak)
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
            //         HttpContext.Session.Remove("SessionId"); // Session'ý da temizleyebiliriz
            //     }
            // }

            ViewData["Title"] = "Sipariþiniz Alýndý";
            return Page();
        }
    }
}