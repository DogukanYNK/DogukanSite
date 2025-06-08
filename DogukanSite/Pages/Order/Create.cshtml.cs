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
    // Authorize etiketini kaldýrýyoruz, çünkü misafirler de bu sayfaya eriþebilmeli.
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
            [Required(ErrorMessage = "Ad Soyad alaný zorunludur.")]
            [Display(Name = "Ad Soyad")]
            public string ContactName { get; set; }

            [Required(ErrorMessage = "Telefon alaný zorunludur.")]
            [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz.")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "E-posta alaný zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Adres alaný zorunludur.")]
            public string Street { get; set; }
            [Required(ErrorMessage = "Þehir alaný zorunludur.")]
            public string City { get; set; }
            [Required(ErrorMessage = "Ýlçe alaný zorunludur.")]
            public string? State { get; set; }
            public string PostalCode { get; set; }
            public string? OrderNotes { get; set; }
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
                WarningMessage = "Sipariþ oluþturmak için sepetinizde ürün bulunmalýdýr.";
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

            // Eðer kullanýcý giriþ yapmýþsa, form bilgilerini önceden doldur
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
                ModelState.AddModelError(string.Empty, "Sepetiniz boþ.");
            }

            // Sayfayý yeniden yüklemek için sepet ve toplam verilerini hazýrla
            CartItems = cartItemsDb.Select(ci => new CartItemViewModel
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name,
                UnitPrice = ci.Product?.DiscountPrice ?? ci.Product.Price,
                Quantity = ci.Quantity,
                ProductImageUrl = ci.Product?.ImageUrl
            }).ToList();
            CalculateTotals();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // --- TRANSACTION BAÞLANGICI ---
            // Bu blok içindeki tüm iþlemler ya hep birlikte baþarýlý olur ya da hep birlikte geri alýnýr.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Sipariþ ana kaydýný oluþtur
                var order = new Models.Order
                {
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    TotalAmount = Total,
                    OrderNotes = OrderInput.OrderNotes, // Artýk isteðe baðlý
                    ShippingContactName = OrderInput.ContactName,
                    ShippingStreet = OrderInput.Street,
                    ShippingCity = OrderInput.City,
                    ShippingState = OrderInput.State,
                    ShippingPostalCode = OrderInput.PostalCode,
                    ShippingCountry = "Türkiye",
                    UserId = cartIdentifier.userId,
                    GuestEmail = cartIdentifier.userId == null ? OrderInput.Email : null
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Order ID'sinin oluþmasý için kaydet

                // 2. Sipariþ kalemlerini oluþtur ve stoklarý düþür
                foreach (var cartItem in cartItemsDb)
                {
                    var productInDb = await _context.Products.FindAsync(cartItem.ProductId);

                    // Son bir stok kontrolü
                    if (productInDb == null || productInDb.Stock < cartItem.Quantity)
                    {
                        await transaction.RollbackAsync(); // Ýþlemi geri al
                        TempData["ErrorMessage"] = $"'{cartItem.Product.Name}' adlý üründe yeterli stok kalmadý, sipariþ iptal edildi.";
                        return RedirectToPage("/Cart/Index");
                    }

                    // STOK DÜÞÜRME ÝÞLEMÝ
                    productInDb.Stock -= cartItem.Quantity;

                    var orderItem = new Models.OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        PriceAtTimeOfPurchase = cartItem.Product.DiscountPrice ?? cartItem.Product.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // 3. Sepeti temizle
                _context.CartItems.RemoveRange(cartItemsDb);

                // 4. Stok ve sipariþ kalemleri ile ilgili tüm deðiþiklikleri veritabanýna iþle
                await _context.SaveChangesAsync();

                // 5. Her þey yolundaysa, iþlemi onayla
                await transaction.CommitAsync();

                _logger.LogInformation("Sipariþ (ID: {OrderId}) baþarýyla oluþturuldu.", order.Id);
                return RedirectToPage("/Order/Success", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                // Beklenmedik bir hata olursa her þeyi geri al
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Sipariþ oluþturulurken bir hata oluþtu.");
                TempData["ErrorMessage"] = "Sipariþiniz oluþturulurken beklenmedik bir hata oluþtu. Lütfen tekrar deneyin.";
                return RedirectToPage("/Cart/Index");
            }
        }

        // Hem misafir hem de üye için sepet kimliðini alan yardýmcý metot
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

        // Gelen kimliðe göre sepeti getiren yardýmcý metot
        private async Task<List<Models.CartItem>> GetCartItems(string? userId, string? sessionId)
        {
            IQueryable<Models.CartItem> query = _context.CartItems.Include(c => c.Product);

            if (userId != null)
            {
                query = query.Where(c => c.ApplicationUserId == userId);
            }
            else if (sessionId != null)
            {
                query = query.Where(c => c.SessionId == sessionId && c.ApplicationUserId == null);
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