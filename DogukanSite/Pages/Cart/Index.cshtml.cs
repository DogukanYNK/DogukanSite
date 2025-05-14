using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DogukanSite.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly ECommerceDbContext _context;

        public IndexModel(ECommerceDbContext context)
        {
            _context = context;
        }

        public List<CartItem> Items { get; set; }
        public decimal Total { get; set; }

        public void OnGet()
        {
            var sessionId = GetSessionId();
            Items = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.SessionId == sessionId)
                .ToList();

            Total = Items.Sum(i => i.Product.Price * i.Quantity);
        }

        public IActionResult OnPostRemove(int id)
        {
            var item = _context.CartItems.Find(id);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToPage();
        }

        private string GetSessionId()
        {
            if (HttpContext.Session.GetString("SessionId") == null)
            {
                var sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
            }
            return HttpContext.Session.GetString("SessionId");
        }
    }

}
