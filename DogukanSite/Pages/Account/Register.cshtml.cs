using DogukanSite.Models; // ApplicationUser için
using Microsoft.AspNetCore.Authentication; // Harici giriþler için
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // IEmailSender için (opsiyonel)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities; // EncodeEmailForLink için
using System.Collections.Generic; // IList için
using System.ComponentModel.DataAnnotations;
using System.Linq;             // Harici giriþler için
using System.Text;             // EncodeEmailForLink için
using System.Text.Encodings.Web;  // HtmlEncoder için (opsiyonel)
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ILogger için eklendi

namespace DogukanSite.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        // private readonly IEmailSender _emailSender; // E-posta onayý için (opsiyonel)

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger/*,
            IEmailSender emailSender*/)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            // _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } // Harici giriþler için

        public class InputModel
        {
            [Required(ErrorMessage = "Ad alaný zorunludur.")]
            [StringLength(50, ErrorMessage = "{0} alaný en az {2} en fazla {1} karakter uzunluðunda olmalýdýr.", MinimumLength = 2)]
            [Display(Name = "Ad")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Soyad alaný zorunludur.")]
            [StringLength(50, ErrorMessage = "{0} alaný en az {2} en fazla {1} karakter uzunluðunda olmalýdýr.", MinimumLength = 2)]
            [Display(Name = "Soyad")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "E-posta alaný zorunludur.")]
            [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Þifre alaný zorunludur.")]
            [StringLength(100, ErrorMessage = "{0} alaný en az {2} en fazla {1} karakter uzunluðunda olmalýdýr.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Þifre")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Þifreyi Doðrula")]
            [Compare("Password", ErrorMessage = "Þifre ile þifre doðrulama alanlarý eþleþmiyor.")]
            public string ConfirmPassword { get; set; }

            // [Range] attribute'ü kaldýrýldý.
            [Display(Name = "Kullanýcý Sözleþmesi ve Gizlilik Politikasýný okudum, kabul ediyorum.")]
            public bool AgreeToTerms { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/"); // Varsayýlan olarak ana sayfaya yönlendir
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // ****** MANUEL KONTROL EKLENDÝ ******
            if (!Input.AgreeToTerms)
            {
                ModelState.AddModelError("Input.AgreeToTerms", "Devam etmek için kullanýcý sözleþmesini kabul etmelisiniz.");
            }
            // ***************************************

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    RegistrationDate = DateTime.UtcNow // YENÝ EKLENEN SATIR
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Yeni kullanýcý þifre ile hesap oluþturdu.");

                    // Yeni kullanýcýya varsayýlan "Customer" rolünü ata
                    await _userManager.AddToRoleAsync(user, "Customer"); // "Customer" rolünün var olduðundan emin olun

                    // E-posta onayý kýsmý yorumda býrakýldý, gerekirse açýlabilir.

                    await _signInManager.SignInAsync(user, isPersistent: false); // Kayýt sonrasý otomatik giriþ
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Bir þeyler ters giderse, formu tekrar göster
            return Page();
        }
    }
}