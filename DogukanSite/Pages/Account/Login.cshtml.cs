using DogukanSite.Models; // ApplicationUser için
using Microsoft.AspNetCore.Authentication; // IAuthenticationSchemeProvider için (Harici giriþler için)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic; // IList için
using System.ComponentModel.DataAnnotations; // DataAnnotations için
using System.Linq;                // Harici giriþler için
using System.Threading.Tasks;     // Asenkron için

namespace DogukanSite.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger; // Loglama için eklendi

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } // Harici giriþler için

        public string ReturnUrl { get; set; } // Kullanýcýyý giriþ sonrasý yönlendirmek için

        [TempData] // Hata mesajýný TempData ile taþýyalým
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "E-posta alaný zorunludur.")]
            [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Þifre alaný zorunludur.")]
            [DataType(DataType.Password)]
            [Display(Name = "Þifre")]
            public string Password { get; set; }

            [Display(Name = "Beni Hatýrla?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/"); // Varsayýlan olarak ana sayfaya yönlendir

            // Mevcut harici çerezi temizle (varsa)
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/"); // Varsayýlan olarak ana sayfaya yönlendir
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid) // Model doðrulamasý yapýldý
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Kullanýcý giriþ yaptý.");
                    return LocalRedirect(returnUrl); // Güvenlik için LocalRedirect kullan
                }
                if (result.RequiresTwoFactor)
                {
                    // Ýki faktörlü kimlik doðrulama için yönlendirme (eðer ayarlýysa)
                    return RedirectToPage("./LoginWith2faPage", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Kullanýcý hesabý kilitlendi.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Giriþ denemesi baþarýsýz. Lütfen e-posta ve þifrenizi kontrol edin.");
                    return Page();
                }
            }

            // Bir þeyler ters giderse, formu tekrar göster
            return Page();
        }

        // Harici giriþler için POST handler (opsiyonel)
        public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            // Harici giriþ saðlayýcýsýna yönlendirme isteði
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }
    }
}