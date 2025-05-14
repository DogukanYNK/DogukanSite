using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DogukanSite.Pages.Order
{
    public class CreateModel : PageModel
    {
        private readonly ECommerceDbContext _context;

        public CreateModel(ECommerceDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string CustomerName { get; set; }

        [BindProperty]
        public string Address { get; set; }

        public List<CartItem> CartItems { get; set; }
        public decimal Total { get; set; }

        public void OnGet()
        {
            var sessionId = GetSessionId();
            CartItems = _context.CartItems.Include(c => c.Product)
                .Where(c => c.SessionId == sessionId).ToList();
            Total = CartItems.Sum(i => i.Product.Price * i.Quantity);
        }

        public IActionResult OnPost()
        {
            var sessionId = GetSessionId();
            var cartItems = _context.CartItems.Include(c => c.Product)
                .Where(c => c.SessionId == sessionId).ToList();

            if (!cartItems.Any())
            {
                return RedirectToPage("/Cart/Index");
            }

            var order = new Models.Order
            {
                CustomerName = CustomerName,
                Address = Address,
                OrderDate = DateTime.Now,
                SessionId = sessionId,
                Items = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems); // Sepeti temizle
            _context.SaveChanges();

            return RedirectToPage("/Order/Success", new { id = order.Id });
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
