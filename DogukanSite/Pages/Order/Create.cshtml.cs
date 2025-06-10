using DogukanSite.Data; // DogukanSiteContext i�in eklendi
using DogukanSite.Models;
using DogukanSite.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Order
{
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly DogukanSiteContext _context; // --- HATA ���N TEKRAR EKLEND� ---

        public CreateModel(
            UserManager<ApplicationUser> userManager,
            ICartService cartService,
            IOrderService orderService,
            DogukanSiteContext context) // --- HATA ���N TEKRAR EKLEND� ---
        {
            _userManager = userManager;
            _cartService = cartService;
            _orderService = orderService;
            _context = context; // --- HATA ���N TEKRAR EKLEND� ---
        }

        [BindProperty]
        public OrderInputModel OrderInput { get; set; }

        public Cart.CartViewModel Cart { get; set; }
        public List<Address> UserAddresses { get; set; } = new List<Address>();

        public class OrderInputModel
        {
            public int SelectedAddressId { get; set; }

            [Required(ErrorMessage = "Ad Soyad alan� zorunludur.")]
            [Display(Name = "Ad Soyad")]
            public string ContactName { get; set; }

            [Required(ErrorMessage = "Telefon numaras� zorunludur.")]
            [Phone(ErrorMessage = "Ge�erli bir telefon numaras� giriniz.")]
            [Display(Name = "Telefon Numaran�z")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "E-posta adresi zorunludur.")]
            [EmailAddress(ErrorMessage = "Ge�erli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta Adresiniz")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Adres alan� zorunludur.")]
            [Display(Name = "A��k Adres (Sokak, Mahalle, No)")]
            public string Street { get; set; }

            [Required(ErrorMessage = "�ehir alan� zorunludur.")]
            [Display(Name = "�ehir")]
            public string City { get; set; }

            [Required(ErrorMessage = "�l�e/Semt alan� zorunludur.")]
            [Display(Name = "�l�e/Semt")]
            public string State { get; set; }

            // --- D�ZELTME: Posta Kodu art�k zorunlu ---
            [Required(ErrorMessage = "Posta kodu zorunludur.")]
            [Display(Name = "Posta Kodu")]
            public string PostalCode { get; set; }

            [Display(Name = "Sipari� Notu (iste�e ba�l�)")]
            public string? OrderNotes { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Cart = await _cartService.GetCartViewModelAsync();
            if (Cart.IsCartEmpty)
            {
                TempData["WarningMessage"] = "Sipari� olu�turmak i�in sepetinizde �r�n bulunmal�d�r.";
                return RedirectToPage("/Cart/Index");
            }

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.Users
                    .Include(u => u.Addresses)
                    .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

                if (user != null)
                {
                    UserAddresses = user.Addresses.ToList();
                    var defaultAddress = UserAddresses.FirstOrDefault(a => a.IsDefaultShipping) ?? UserAddresses.FirstOrDefault();

                    OrderInput = new OrderInputModel
                    {
                        SelectedAddressId = defaultAddress?.Id ?? 0,
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
            // --- HATA BURADAYDI, ARTIK _context GE�ERL� ---
            if (!ModelState.IsValid)
            {
                // Hata durumunda sayfay� yeniden haz�rlamak i�in verileri tekrar y�kle
                Cart = await _cartService.GetCartViewModelAsync();
                if (User.Identity.IsAuthenticated)
                {
                    UserAddresses = await _context.Addresses.Where(a => a.UserId == _userManager.GetUserId(User)).ToListAsync();
                }
                return Page();
            }

            var userId = User.Identity.IsAuthenticated ? _userManager.GetUserId(User) : null;
            var sessionId = userId == null ? HttpContext.Session.GetString("SessionId") : null;

            var result = await _orderService.CreateOrderFromCartAsync(OrderInput, userId, sessionId);

            if (result.Success)
            {
                return RedirectToPage("/Order/Success", new { orderId = result.CreatedOrderId });
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToPage("/Cart/Index");
            }
        }
    }
}