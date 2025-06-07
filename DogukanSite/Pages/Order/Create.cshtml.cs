using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    // Authorize etiketini kald�r�yoruz, ��nk� misafirler de bu sayfaya eri�ebilmeli.
    public class CreateModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager, ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public OrderInputModel OrderInput { get; set; }

        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }

        [TempData]
        public string WarningMessage { get; set; }

        public class OrderInputModel
        {
            [Required(ErrorMessage = "Ad Soyad alan� zorunludur.")]
            [Display(Name = "Ad Soyad")]
            public string ContactName { get; set; }

            [Required(ErrorMessage = "Telefon alan� zorunludur.")]
            [Phone(ErrorMessage = "Ge�erli bir telefon numaras� giriniz.")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "E-posta alan� zorunludur.")]
            [EmailAddress(ErrorMessage = "Ge�erli bir e-posta adresi giriniz.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Adres alan� zorunludur.")]
            public string Street { get; set; }
            [Required(ErrorMessage = "�ehir alan� zorunludur.")]
            public string City { get; set; }
            [Required(ErrorMessage = "�l�e alan� zorunludur.")]
            public string State { get; set; }
            public string PostalCode { get; set; }
            public string OrderNotes { get; set; }
        }

        public class CartItemViewModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductImageUrl { get; set; }
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal TotalPrice => UnitPrice * Quantity;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var cartIdentifier = GetCartIdentifier();
            var cartItemsDb = await GetCartItems(cartIdentifier.userId, cartIdentifier.sessionId);

            if (!cartItemsDb.Any())
            {
                WarningMessage = "Sipari� olu�turmak i�in sepetinizde �r�n bulunmal�d�r.";
                return RedirectToPage("/Cart/Index");
            }

            CartItems = cartItemsDb.Select(ci => new CartItemViewModel
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name,
                ProductImageUrl = ci.Product?.ImageUrl,
                UnitPrice = ci.Product?.DiscountPrice ?? ci.Product.Price,
                Quantity = ci.Quantity
            }).ToList();

            CalculateTotals();

            // E�er kullan�c� giri� yapm��sa, form bilgilerini �nceden doldur
            if (cartIdentifier.userId != null)
            {
                var user = await _userManager.Users.Include(u => u.Addresses).FirstOrDefaultAsync(u => u.Id == cartIdentifier.userId);
                if (user != null)
                {
                    var defaultAddress = user.Addresses?.FirstOrDefault(a => a.IsDefaultShipping) ?? user.Addresses?.FirstOrDefault();
                    OrderInput = new OrderInputModel
                    {
                        ContactName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Street = defaultAddress?.Street,
                        City = defaultAddress?.City,
                        State = defaultAddress?.State,
                        PostalCode = defaultAddress?.PostalCode
                    };
                }
            }

            OrderInput ??= new OrderInputModel();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var cartIdentifier = GetCartIdentifier();
            var cartItemsDb = await GetCartItems(cartIdentifier.userId, cartIdentifier.sessionId);

            if (!cartItemsDb.Any())
            {
                ModelState.AddModelError(string.Empty, "Sepetiniz bo� veya zaman a��m�na u�rad�.");
            }

            CartItems = cartItemsDb.Select(ci => new CartItemViewModel
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name,
                ProductImageUrl = ci.Product?.ImageUrl,
                UnitPrice = ci.Product?.DiscountPrice ?? ci.Product.Price,
                Quantity = ci.Quantity
            }).ToList();
            CalculateTotals();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var order = new Models.Order
            {
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalAmount = Total,
                OrderNotes = OrderInput.OrderNotes,
                ShippingContactName = OrderInput.ContactName,
                ShippingStreet = OrderInput.Street,
                ShippingCity = OrderInput.City,
                ShippingState = OrderInput.State,
                ShippingPostalCode = OrderInput.PostalCode,
                ShippingCountry = "T�rkiye",
                OrderItems = cartItemsDb.Select(ci => new Models.OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    PriceAtTimeOfPurchase = ci.Product.DiscountPrice ?? ci.Product.Price
                }).ToList()
            };

            // Sipari�i kullan�c�ya veya misafire ba�la
            if (cartIdentifier.userId != null)
            {
                order.UserId = cartIdentifier.userId;
            }
            else
            {
                order.GuestEmail = OrderInput.Email;
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItemsDb);
            await _context.SaveChangesAsync();

            // Misafir sepetini de temizle
            if (cartIdentifier.userId == null)
            {
                HttpContext.Session.Remove("SessionId");
            }

            _logger.LogInformation("Sipari� (ID: {OrderId}) ba�ar�yla olu�turuldu.", order.Id);
            return RedirectToPage("/Order/Success", new { orderId = order.Id });
        }

        // Hem misafir hem de �ye i�in sepet kimli�ini alan yard�mc� metot
        private (string? userId, string? sessionId) GetCartIdentifier()
        {
            if (User.Identity.IsAuthenticated)
            {
                return (_userManager.GetUserId(User), null);
            }

            string sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
            }
            return (null, sessionId);
        }

        // Gelen kimli�e g�re sepeti getiren yard�mc� metot
        private async Task<List<Models.CartItem>> GetCartItems(string? userId, string? sessionId)
        {
            IQueryable<Models.CartItem> query = _context.CartItems.Include(c => c.Product);

            if (userId != null)
            {
                query = query.Where(c => c.UserId == userId);
            }
            else if (sessionId != null)
            {
                query = query.Where(c => c.SessionId == sessionId && c.UserId == null);
            }
            else
            {
                return new List<Models.CartItem>();
            }

            return await query.Where(c => c.Product != null && c.Product.Stock > 0).ToListAsync();
        }

        private void CalculateTotals()
        {
            Subtotal = CartItems.Sum(i => i.TotalPrice);
            ShippingCost = (Subtotal > 0 && Subtotal < 500m) ? 49.99m : 0m;
            Total = Subtotal + ShippingCost;
        }
    }
}