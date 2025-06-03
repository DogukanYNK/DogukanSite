using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System; // DateTime için eklendi
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    public class CreateModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(DogukanSiteContext context,
                           UserManager<ApplicationUser> userManager,
                           ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public OrderInputModel OrderInput { get; set; }

        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>(); // Baþlangýç deðeri atandý
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        // Kupon ve indirimle ilgili alanlar CartModel'dan buraya da taþýnabilir veya ortak bir servisten okunabilir.
        // Þimdilik basit tutuyorum, sadece temel toplamlar var.
        public decimal Total { get; set; }

        [TempData]
        public string WarningMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; } // Genel hatalar için

        public class OrderInputModel
        {
            [Required(ErrorMessage = "Ad Soyad alaný zorunludur.")]
            [Display(Name = "Ad Soyad")]
            [StringLength(100)]
            public string CustomerName { get; set; }

            [Required(ErrorMessage = "Telefon alaný zorunludur.")]
            [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz.")]
            [Display(Name = "Telefon Numarasý")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "E-posta alaný zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta Adresi")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Adres alaný zorunludur.")]
            [Display(Name = "Teslimat Adresi")]
            [StringLength(250)]
            public string AddressLine1 { get; set; }

            [Display(Name = "Adres Satýrý 2 (Opsiyonel)")]
            [StringLength(100)]
            public string AddressLine2 { get; set; }

            [Required(ErrorMessage = "Þehir alaný zorunludur.")]
            [Display(Name = "Þehir")]
            [StringLength(50)]
            public string City { get; set; }

            [Required(ErrorMessage = "Ýlçe alaný zorunludur.")]
            [Display(Name = "Ýlçe")]
            [StringLength(50)]
            public string District { get; set; }

            [Display(Name = "Posta Kodu (Opsiyonel)")]
            [StringLength(10)]
            public string PostalCode { get; set; }

            [StringLength(500)]
            [Display(Name = "Sipariþ Notu (Opsiyonel)")]
            public string OrderNotes { get; set; }
        }

        public class CartItemViewModel // Bu ViewModel CartModel'daki ile ayný olmalý
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductImageUrl { get; set; }
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal TotalPrice => UnitPrice * Quantity;
        }

        private string? GetCurrentUserId()
        {
            return User.Identity.IsAuthenticated ? _userManager.GetUserId(User) : null;
        }

        private string GetSessionId(bool createIfNull = true) // CartModel'daki ile ayný olmalý
        {
            var currentPath = HttpContext.Request.Path;
            _logger.LogInformation("OrderModel.GetSessionId çaðrýldý. Request Path: {Path}, createIfNull: {CreateIfNull}", currentPath, createIfNull);

            if (HttpContext.Session == null)
            {
                _logger.LogError("OrderModel.GetSessionId: HttpContext.Session IS NULL! Session middleware (app.UseSession()) Program.cs'te doðru yapýlandýrýlmamýþ veya çaðrýlmamýþ olabilir. Path: {Path}", currentPath);
                throw new InvalidOperationException("HttpContext.Session is null. Session middleware doðru yapýlandýrýlmamýþ olabilir.");
            }

            if (!HttpContext.Session.IsAvailable)
            {
                _logger.LogWarning("OrderModel.GetSessionId: HttpContext.Session.IsAvailable == false. Session yüklenemedi veya kullanýlamýyor. Path: {Path}", currentPath);
                if (!createIfNull)
                {
                    _logger.LogWarning("OrderModel.GetSessionId: createIfNull false olduðu için boþ string dönülüyor. Path: {Path}", currentPath);
                    return string.Empty; // Veya null, çaðýran yere göre
                }
                throw new InvalidOperationException($"Session is not available for path {currentPath}. Ensure session is configured and enabled, and app.UseSession() is correctly placed in Program.cs.");
            }

            string? sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId) && createIfNull)
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
                _logger.LogInformation("OrderModel.GetSessionId: Yeni SessionId üretildi ve set edildi: {SessionId}. Path: {Path}", sessionId, currentPath);
            }
            else if (string.IsNullOrEmpty(sessionId) && !createIfNull)
            {
                _logger.LogWarning("OrderModel.GetSessionId: SessionId null/empty ve createIfNull false. Boþ string dönülüyor. Path: {Path}", currentPath);
                return string.Empty;
            }
            else
            {
                _logger.LogInformation("OrderModel.GetSessionId: Mevcut SessionId alýndý: {SessionId}. Path: {Path}", sessionId, currentPath);
            }
            return sessionId ?? string.Empty;
        }

        private async Task<List<Models.CartItem>> GetCartItemsFromDbAsync()
        {
            string? userId = GetCurrentUserId();
            IQueryable<Models.CartItem> query;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("GetCartItemsFromDbAsync (OrderModel): Giriþ yapmýþ kullanýcý ({UserId}) için sepet sorgulanýyor.", userId);
                query = _context.CartItems.Include(c => c.Product)
                                .Where(c => c.ApplicationUserId == userId);
            }
            else
            {
                string sessionId;
                try
                {
                    // Anonim kullanýcý için session ID al/oluþtur. Order/Create'e anonim gelinmemeli ama önlem.
                    sessionId = GetSessionId(createIfNull: true);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "GetCartItemsFromDbAsync (OrderModel): Anonim kullanýcý için GetSessionId çaðrýlýrken session alýnamadý.");
                    return new List<Models.CartItem>(); // Boþ liste dön, OnGet/OnPost bunu handle etmeli
                }

                if (string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogWarning("GetCartItemsFromDbAsync (OrderModel): Anonim kullanýcý için SessionId boþ, boþ sepet dönülüyor.");
                    return new List<Models.CartItem>();
                }
                _logger.LogInformation("GetCartItemsFromDbAsync (OrderModel): Anonim kullanýcý (SessionId: {SessionId}) için sepet sorgulanýyor.", sessionId);
                query = _context.CartItems.Include(c => c.Product)
                                .Where(c => c.SessionId == sessionId && c.ApplicationUserId == null);
            }
            return await query.Where(c => c.Product != null && c.Product.Stock > 0).ToListAsync();
        }


        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("Order/Create OnGetAsync çaðrýldý.");

            var cartItemsDb = await GetCartItemsFromDbAsync();
            _logger.LogInformation("OnGetAsync: Veritabanýndan {Count} adet sepet ürünü çekildi.", cartItemsDb.Count);

            if (!cartItemsDb.Any())
            {
                _logger.LogWarning("OnGetAsync: Sepet boþ veya eþleþen ürün bulunamadý. Cart/Index'e yönlendiriliyor.");
                WarningMessage = "Sipariþ oluþturmak için sepetinizde ürün bulunmalýdýr.";
                return RedirectToPage("/Cart/Index");
            }

            CartItems = cartItemsDb.Select(ci => new CartItemViewModel
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name,
                ProductImageUrl = ci.Product?.ImageUrl,
                UnitPrice = ci.Product?.Price ?? 0,
                Quantity = ci.Quantity
            }).ToList();

            CalculateTotals(); // Kupon/indirim mantýðý yoksa sadece Subtotal, Shipping, Total hesaplar
            _logger.LogInformation("OnGetAsync: Sepet toplamlarý hesaplandý: Subtotal={Subtotal}, Shipping={ShippingCost}, Total={Total}", Subtotal, ShippingCost, Total);

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    _logger.LogInformation("OnGetAsync: Giriþ yapmýþ kullanýcý ({UserName}) için bilgiler forma dolduruluyor.", user.UserName);
                    OrderInput = new OrderInputModel
                    {
                        CustomerName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        // Adres bilgileri ApplicationUser modelinizde varsa buraya ekleyebilirsiniz.
                        // AddressLine1 = user.DefaultAddressLine1, 
                        // City = user.DefaultCity,
                        // District = user.DefaultDistrict,
                        // PostalCode = user.DefaultPostalCode
                    };
                }
            }
            OrderInput ??= new OrderInputModel();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Order/Create OnPostAsync çaðrýldý.");

            var cartItemsDb = await GetCartItemsFromDbAsync(); // Sepeti tekrar yükle ve doðrula
            _logger.LogInformation("OnPostAsync: Veritabanýndan {Count} adet sepet ürünü çekildi.", cartItemsDb.Count);

            if (!cartItemsDb.Any())
            {
                _logger.LogWarning("OnPostAsync: Sepet boþ bulundu. Form gönderilemiyor.");
                ModelState.AddModelError(string.Empty, "Sepetiniz boþ veya zaman aþýmýna uðradý. Lütfen sepetinizi kontrol edip tekrar deneyin.");
                // Sayfayý tekrar göstermek için CartItems ve Total'ý set etmemiz lazým.
                // GetCartItemsFromDbAsync zaten boþ liste dönecek, CalculateTotals da sýfýrlarý hesaplayacak.
                CartItems = new List<CartItemViewModel>();
                CalculateTotals();
                return Page();
            }

            // Model geçerli olmasa bile, sayfada sepeti ve toplamlarý göstermek için bu bilgileri yüklemeliyiz.
            CartItems = cartItemsDb.Select(ci => new CartItemViewModel
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name,
                ProductImageUrl = ci.Product?.ImageUrl,
                UnitPrice = ci.Product?.Price ?? 0,
                Quantity = ci.Quantity
            }).ToList();
            CalculateTotals(); // Kupon/indirim mantýðý yoksa sadece Subtotal, Shipping, Total hesaplar

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("OnPostAsync: ModelState geçerli deðil. Hatalar: {Errors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            // Sipariþ Oluþturma Mantýðý
            var order = new Models.Order
            {
                OrderDate = DateTime.UtcNow,
                // SessionId'yi sadece anonim kullanýcýlar için kaydetmek daha mantýklý olabilir.
                // Eðer kullanýcý giriþ yapmýþsa, UserId daha önemlidir.
                CustomerName = OrderInput.CustomerName,
                ShippingAddress = $"{OrderInput.AddressLine1}, {(string.IsNullOrEmpty(OrderInput.AddressLine2) ? "" : OrderInput.AddressLine2 + ", ")}{OrderInput.District} / {OrderInput.City}{(string.IsNullOrEmpty(OrderInput.PostalCode) ? "" : " " + OrderInput.PostalCode)}".Trim(),
                PhoneNumber = OrderInput.PhoneNumber,
                Email = OrderInput.Email,
                OrderNotes = OrderInput.OrderNotes,
                OrderTotal = Total, // CalculateTotals'dan gelen güncel toplam
                OrderStatus = "Pending",
                Items = cartItemsDb.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Product?.Price ?? 0
                }).ToList()
            };

            if (User.Identity.IsAuthenticated)
            {
                order.UserId = GetCurrentUserId();
            }
            else
            {
                // Eðer anonim kullanýcý sipariþ verebiliyorsa, SessionId'yi sipariþe kaydet
                // Ancak bu durumda GetSessionId'nin yeni ID üretmediðinden emin olmalýyýz.
                // En iyisi anonim sipariþe izin vermemek veya çok dikkatli yönetmek.
                // Þimdilik GetSessionId'nin mevcut (veya yeni üretilmiþ) ID'yi vereceðini varsayýyoruz.
                order.SessionId = GetSessionId(createIfNull: false); // Var olaný al, yoksa boþ. Boþsa sipariþe nasýl baðlanacak?
                                                                     // Bu mantýk gözden geçirilmeli eðer anonim sipariþ olacaksa.
                                                                     // Güvenlik açýsýndan, bu noktada session ID'nin en baþtan beri tutarlý olmasý beklenir.
                if (string.IsNullOrEmpty(order.SessionId) && string.IsNullOrEmpty(order.UserId))
                {
                    _logger.LogError("OnPostAsync: Sipariþ için ne UserId ne de geçerli bir SessionId bulunamadý!");
                    ModelState.AddModelError(string.Empty, "Sipariþiniz oluþturulurken bir kimlik doðrulama sorunu yaþandý.");
                    return Page(); // Hata ile sayfayý göster
                }
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItemsDb); // Sipariþ sonrasý sepeti temizle

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sipariþ (ID: {OrderId}) baþarýyla oluþturuldu ve sepet temizlendi. User: {UserId}, Session: {SessionId}", order.Id, order.UserId, order.SessionId);

            // Sipariþ sonrasý anonim kullanýcý session'ýndaki "SessionId" anahtarýný temizleyebiliriz, çünkü sepeti boþaldý.
            // Ama zaten sepet temizlendiði için bir sonraki GetSessionId yeni üretecektir.
            // HttpContext.Session.Remove("SessionId"); // Bu, bir sonraki sepet için yeni ID üretilmesini saðlar.

            return RedirectToPage("/Order/Success", new { orderId = order.Id });
        }

        // CalculateTotals metodunu CartModel'daki ile ayný veya benzer yapýda tutun.
        // Kupon mantýðý burada yoksa, sadece ara toplam, kargo ve genel toplamý hesaplar.
        private void CalculateTotals()
        {
            if (CartItems == null) CartItems = new List<CartItemViewModel>();
            Subtotal = CartItems.Sum(i => i.TotalPrice);
            // Örnek kargo mantýðý (CartModel'daki gibi)
            ShippingCost = (Subtotal > 0 && Subtotal < 250m && Subtotal != 0) ? 39.99m : 0m; // 250 TL altý için kargo
            Total = Subtotal + ShippingCost;
        }
    }
}