using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    public class SuccessModel : PageModel
    {
        private readonly DogukanSiteContext _context;

        public SuccessModel(DogukanSiteContext context)
        {
            _context = context;
        }

        public Models.Order CurrentOrder { get; set; }
        public string CustomerEmail { get; set; }

        // D�ZELTME: OrderId �zelli�ini geri ekledik.
        public int OrderId { get; set; }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            if (orderId == 0)
            {
                return NotFound("Sipari� ID bulunamad�.");
            }

            // D�ZELTME: Gelen orderId'yi �zelli�imize at�yoruz.
            OrderId = orderId;

            CurrentOrder = await _context.Orders
                                         .Include(o => o.User)
                                         .FirstOrDefaultAsync(o => o.Id == orderId);
            if (CurrentOrder == null)
            {
                // Sipari� bulunamasa bile OrderId'yi bildi�imiz i�in sayfay� g�sterebiliriz.
                ViewData["Title"] = "Sipari�iniz Al�nd�";
                return Page();
            }

            if (CurrentOrder.User != null)
            {
                CustomerEmail = CurrentOrder.User.Email;
            }
            else
            {
                CustomerEmail = CurrentOrder.GuestEmail;
            }

            ViewData["Title"] = "Sipari�iniz Al�nd�";
            return Page();
        }
    }
}