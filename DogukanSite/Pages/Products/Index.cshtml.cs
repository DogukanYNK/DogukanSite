using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogukanSite.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly ECommerceDbContext _context;

        public IndexModel(ECommerceDbContext context)
        {
            _context = context;
        }

        public List<Models.Product> Products { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

        public List<string> AllCategories { get; set; }

        public void OnGet()
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(p => p.Name.ToLower().Contains(SearchTerm.ToLower()));
            }

            if (!string.IsNullOrEmpty(Category))
            {
                query = query.Where(p => p.Category.ToLower() == Category.ToLower());
            }

            Products = query.ToList();
            AllCategories = _context.Products.Select(p => p.Category).Distinct().ToList();
        }

        public IActionResult OnPostAddToCart(int productId)
        {
            var sessionId = HttpContext.Session.GetString("SessionId") ?? Guid.NewGuid().ToString();
            HttpContext.Session.SetString("SessionId", sessionId);

            var existingItem = _context.CartItems
                .FirstOrDefault(c => c.ProductId == productId && c.SessionId == sessionId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                var cartItem = new CartItem
                {
                    ProductId = productId,
                    Quantity = 1,
                    SessionId = sessionId
                };
                _context.CartItems.Add(cartItem);
            }

            _context.SaveChanges();
            return RedirectToPage();
        }
    }

}
