using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System; // DateTime i�in eklendi
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

        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>(); // Ba�lang�� de�eri atand�
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        // Kupon ve indirimle ilgili alanlar CartModel'dan buraya da ta��nabilir veya ortak bir servisten okunabilir.
        // �imdilik basit tutuyorum, sadece temel toplamlar var.
        public decimal Total { get; set; }

        [TempData]
        public string WarningMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; } // Genel hatalar i�in

        public class OrderInputModel
        {
            [Required(ErrorMessage = "Ad Soyad alan� zorunludur.")]
            [Display(Name = "Ad Soyad")]
            [StringLength(100)]
            public string CustomerName { get; set; }

            [Required(ErrorMessage = "Telefon alan� zorunludur.")]
            [Phone(ErrorMessage = "Ge�erli bir telefon numaras� giriniz.")]
            [Display(Name = "Telefon Numaras�")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "E-posta alan� zorunludur.")]
            [EmailAddress(ErrorMessage = "Ge�erli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta Adresi")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Adres alan� zorunludur.")]
            [Display(Name = "Teslimat Adresi")]
            [StringLength(250)]
            public string AddressLine1 { get; set; }

            [Display(Name = "Adres Sat�r� 2 (Opsiyonel)")]
            [StringLength(100)]
            public string AddressLine2 { get; set; }

            [Required(ErrorMessage = "�ehir alan� zorunludur.")]
            [Display(Name = "�ehir")]
            [StringLength(50)]
            public string City { get; set; }

            [Required(ErrorMessage = "�l�e alan� zorunludur.")]
            [Display(Name = "�l�e")]
            [StringLength(50)]
            public string District { get; set; }

            [Display(Name = "Posta Kodu (Opsiyonel)")]
            [StringLength(10)]
            public string PostalCode { get; set; }

            [StringLength(500)]
            [Display(Name = "Sipari� Notu (Opsiyonel)")]
            public string OrderNotes { get; set; }
        }

        public class CartItemViewModel // Bu ViewModel CartModel'daki ile ayn� olmal�
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

        private string GetSessionId(bool createIfNull = true) // CartModel'daki ile ayn� olmal�
        {
            var currentPath = HttpContext.Request.Path;
            _logger.LogInformation("OrderModel.GetSessionId �a�r�ld�. Request Path: {Path}, createIfNull: {CreateIfNull}", currentPath, createIfNull);

            if (HttpContext.Session == null)
            {
                _logger.LogError("OrderModel.GetSessionId: HttpContext.Session IS NULL! Session middleware (app.UseSession()) Program.cs'te do�ru yap�land�r�lmam�� veya �a�r�lmam�� olabilir. Path: {Path}", currentPath);
                throw new InvalidOperationException("HttpContext.Session is null. Session middleware do�ru yap�land�r�lmam�� olabilir.");
            }

            if (!HttpContext.Session.IsAvailable)
            {
                _logger.LogWarning("OrderModel.GetSessionId: HttpContext.Session.IsAvailable == false. Session y�klenemedi veya kullan�lam�yor. Path: {Path}", currentPath);
                if (!createIfNull)
                {
                    _logger.LogWarning("OrderModel.GetSessionId: createIfNull false oldu�u i�in bo� string d�n�l�yor. Path: {Path}", currentPath);
                    return string.Empty; // Veya null, �a��ran yere g�re
                }
                throw new InvalidOperationException($"Session is not available for path {currentPath}. Ensure session is configured and enabled, and app.UseSession() is correctly placed in Program.cs.");
            }

            string? sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId) && createIfNull)
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
                _logger.LogInformation("OrderModel.GetSessionId: Yeni SessionId �retildi ve set edildi: {SessionId}. Path: {Path}", sessionId, currentPath);
            }
            else if (string.IsNullOrEmpty(sessionId) && !createIfNull)
            {
                _logger.LogWarning("OrderModel.GetSessionId: SessionId null/empty ve createIfNull false. Bo� string d�n�l�yor. Path: {Path}", currentPath);
                return string.Empty;
            }
            else
            {
                _logger.LogInformation("OrderModel.GetSessionId: Mevcut SessionId al�nd�: {SessionId}. Path: {Path}", sessionId, currentPath);
            }
            return sessionId ?? string.Empty;
        }

        private async Task<List<Models.CartItem>> GetCartItemsFromDbAsync()
        {
            string? userId = GetCurrentUserId();
            IQueryable<Models.CartItem> query;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("GetCartItemsFromDbAsync (OrderModel): Giri� yapm�� kullan�c� ({UserId}) i�in sepet sorgulan�yor.", userId);
                query = _context.CartItems.Include(c => c.Product)
                                .Where(c => c.ApplicationUserId == userId);
            }
            else
            {
                string sessionId;
                try
                {
                    // Anonim kullan�c� i�in session ID al/olu�tur. Order/Create'e anonim gelinmemeli ama �nlem.
                    sessionId = GetSessionId(createIfNull: true);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "GetCartItemsFromDbAsync (OrderModel): Anonim kullan�c� i�in GetSessionId �a�r�l�rken session al�namad�.");
                    return new List<Models.CartItem>(); // Bo� liste d�n, OnGet/OnPost bunu handle etmeli
                }

                if (string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogWarning("GetCartItemsFromDbAsync (OrderModel): Anonim kullan�c� i�in SessionId bo�, bo� sepet d�n�l�yor.");
                    return new List<Models.CartItem>();
                }
                _logger.LogInformation("GetCartItemsFromDbAsync (OrderModel): Anonim kullan�c� (SessionId: {SessionId}) i�in sepet sorgulan�yor.", sessionId);
                query = _context.CartItems.Include(c => c.Product)
                                .Where(c => c.SessionId == sessionId && c.ApplicationUserId == null);
            }
            return await query.Where(c => c.Product != null && c.Product.Stock > 0).ToListAsync();
        }


        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("Order/Create OnGetAsync �a�r�ld�.");

            var cartItemsDb = await GetCartItemsFromDbAsync();
            _logger.LogInformation("OnGetAsync: Veritaban�ndan {Count} adet sepet �r�n� �ekildi.", cartItemsDb.Count);

            if (!cartItemsDb.Any())
            {
                _logger.LogWarning("OnGetAsync: Sepet bo� veya e�le�en �r�n bulunamad�. Cart/Index'e y�nlendiriliyor.");
                WarningMessage = "Sipari� olu�turmak i�in sepetinizde �r�n bulunmal�d�r.";
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

            CalculateTotals(); // Kupon/indirim mant��� yoksa sadece Subtotal, Shipping, Total hesaplar
            _logger.LogInformation("OnGetAsync: Sepet toplamlar� hesapland�: Subtotal={Subtotal}, Shipping={ShippingCost}, Total={Total}", Subtotal, ShippingCost, Total);

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    _logger.LogInformation("OnGetAsync: Giri� yapm�� kullan�c� ({UserName}) i�in bilgiler forma dolduruluyor.", user.UserName);
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
            _logger.LogInformation("Order/Create OnPostAsync �a�r�ld�.");

            var cartItemsDb = await GetCartItemsFromDbAsync(); // Sepeti tekrar y�kle ve do�rula
            _logger.LogInformation("OnPostAsync: Veritaban�ndan {Count} adet sepet �r�n� �ekildi.", cartItemsDb.Count);

            if (!cartItemsDb.Any())
            {
                _logger.LogWarning("OnPostAsync: Sepet bo� bulundu. Form g�nderilemiyor.");
                ModelState.AddModelError(string.Empty, "Sepetiniz bo� veya zaman a��m�na u�rad�. L�tfen sepetinizi kontrol edip tekrar deneyin.");
                // Sayfay� tekrar g�stermek i�in CartItems ve Total'� set etmemiz laz�m.
                // GetCartItemsFromDbAsync zaten bo� liste d�necek, CalculateTotals da s�f�rlar� hesaplayacak.
                CartItems = new List<CartItemViewModel>();
                CalculateTotals();
                return Page();
            }

            // Model ge�erli olmasa bile, sayfada sepeti ve toplamlar� g�stermek i�in bu bilgileri y�klemeliyiz.
            CartItems = cartItemsDb.Select(ci => new CartItemViewModel
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name,
                ProductImageUrl = ci.Product?.ImageUrl,
                UnitPrice = ci.Product?.Price ?? 0,
                Quantity = ci.Quantity
            }).ToList();
            CalculateTotals(); // Kupon/indirim mant��� yoksa sadece Subtotal, Shipping, Total hesaplar

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("OnPostAsync: ModelState ge�erli de�il. Hatalar: {Errors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            // Sipari� Olu�turma Mant���
            var order = new Models.Order
            {
                OrderDate = DateTime.UtcNow,
                // SessionId'yi sadece anonim kullan�c�lar i�in kaydetmek daha mant�kl� olabilir.
                // E�er kullan�c� giri� yapm��sa, UserId daha �nemlidir.
                CustomerName = OrderInput.CustomerName,
                ShippingAddress = $"{OrderInput.AddressLine1}, {(string.IsNullOrEmpty(OrderInput.AddressLine2) ? "" : OrderInput.AddressLine2 + ", ")}{OrderInput.District} / {OrderInput.City}{(string.IsNullOrEmpty(OrderInput.PostalCode) ? "" : " " + OrderInput.PostalCode)}".Trim(),
                PhoneNumber = OrderInput.PhoneNumber,
                Email = OrderInput.Email,
                OrderNotes = OrderInput.OrderNotes,
                OrderTotal = Total, // CalculateTotals'dan gelen g�ncel toplam
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
                // E�er anonim kullan�c� sipari� verebiliyorsa, SessionId'yi sipari�e kaydet
                // Ancak bu durumda GetSessionId'nin yeni ID �retmedi�inden emin olmal�y�z.
                // En iyisi anonim sipari�e izin vermemek veya �ok dikkatli y�netmek.
                // �imdilik GetSessionId'nin mevcut (veya yeni �retilmi�) ID'yi verece�ini varsay�yoruz.
                order.SessionId = GetSessionId(createIfNull: false); // Var olan� al, yoksa bo�. Bo�sa sipari�e nas�l ba�lanacak?
                                                                     // Bu mant�k g�zden ge�irilmeli e�er anonim sipari� olacaksa.
                                                                     // G�venlik a��s�ndan, bu noktada session ID'nin en ba�tan beri tutarl� olmas� beklenir.
                if (string.IsNullOrEmpty(order.SessionId) && string.IsNullOrEmpty(order.UserId))
                {
                    _logger.LogError("OnPostAsync: Sipari� i�in ne UserId ne de ge�erli bir SessionId bulunamad�!");
                    ModelState.AddModelError(string.Empty, "Sipari�iniz olu�turulurken bir kimlik do�rulama sorunu ya�and�.");
                    return Page(); // Hata ile sayfay� g�ster
                }
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItemsDb); // Sipari� sonras� sepeti temizle

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sipari� (ID: {OrderId}) ba�ar�yla olu�turuldu ve sepet temizlendi. User: {UserId}, Session: {SessionId}", order.Id, order.UserId, order.SessionId);

            // Sipari� sonras� anonim kullan�c� session'�ndaki "SessionId" anahtar�n� temizleyebiliriz, ��nk� sepeti bo�ald�.
            // Ama zaten sepet temizlendi�i i�in bir sonraki GetSessionId yeni �retecektir.
            // HttpContext.Session.Remove("SessionId"); // Bu, bir sonraki sepet i�in yeni ID �retilmesini sa�lar.

            return RedirectToPage("/Order/Success", new { orderId = order.Id });
        }

        // CalculateTotals metodunu CartModel'daki ile ayn� veya benzer yap�da tutun.
        // Kupon mant��� burada yoksa, sadece ara toplam, kargo ve genel toplam� hesaplar.
        private void CalculateTotals()
        {
            if (CartItems == null) CartItems = new List<CartItemViewModel>();
            Subtotal = CartItems.Sum(i => i.TotalPrice);
            // �rnek kargo mant��� (CartModel'daki gibi)
            ShippingCost = (Subtotal > 0 && Subtotal < 250m && Subtotal != 0) ? 39.99m : 0m; // 250 TL alt� i�in kargo
            Total = Subtotal + ShippingCost;
        }
    }
}