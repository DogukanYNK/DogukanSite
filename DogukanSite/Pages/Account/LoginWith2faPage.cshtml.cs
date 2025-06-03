// Pages/Account/LoginWith2faPage.cshtml.cs
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization; // [AllowAnonymous] gerekebilir
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [AllowAnonymous] // Bu sayfa, kullanýcý henüz tam olarak giriþ yapmadýðý için anonim eriþime izin vermelidir.
    public class LoginWith2faPageModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager; // Hata mesajýnda kullanýcý adý göstermek için (opsiyonel)
        private readonly ILogger<LoginWith2faPageModel> _logger;

        public LoginWith2faPageModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginWith2faPageModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public bool RememberMe { get; set; } // Login sayfasýndan gelen "Beni Hatýrla" durumu

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Doðrulama kodu zorunludur.")]
            [StringLength(7, MinimumLength = 6, ErrorMessage = "{0} 6 ile 7 karakter arasýnda olmalýdýr.")]
            [DataType(DataType.Text)]
            [Display(Name = "Doðrulama Kodu")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Bu makineyi hatýrla")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            // Kullanýcýnýn þifre adýmýný geçtiðinden emin ol
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                // Bu durum, kullanýcýnýn doðrudan bu sayfaya gelmeye çalýþtýðý anlamýna gelir.
                // Ana giriþ sayfasýna yönlendirmek daha mantýklý.
                _logger.LogWarning("LoginWith2faPage.OnGetAsync: Ýki aþamalý doðrulama için kullanýcý bulunamadý. Muhtemelen þifre adýmý atlandý.");
                ErrorMessage = "Ýki aþamalý doðrulama için geçerli bir kullanýcý oturumu bulunamadý. Lütfen tekrar giriþ yapmayý deneyin.";
                return RedirectToPage("./Login");
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            RememberMe = rememberMe;
            Input = new InputModel(); // Formu boþ baþlat

            _logger.LogInformation("Kullanýcý ({UserId}) için LoginWith2faPage yüklendi.", user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            RememberMe = rememberMe; // Login sayfasýndan gelen deðeri koru

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("LoginWith2faPage.OnPostAsync: Ýki aþamalý doðrulama için kullanýcý bulunamadý (POST).");
                ErrorMessage = "Ýki aþamalý doðrulama oturumunuz zaman aþýmýna uðramýþ olabilir. Lütfen tekrar giriþ yapmayý deneyin.";
                // Burada direkt RedirectToPage("./Login") yerine ErrorMessage ile Page() döndürmek,
                // kullanýcýnýn girdiði formu kaybetmemesini saðlar (eðer bir hata varsa).
                // Ancak session/cookie temelli 2FA kullanýcýsý için bu, genellikle Login'e yönlendirmeyi gerektirir.
                return RedirectToPage("./Login", new { ReturnUrl = ReturnUrl });
            }

            var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, RememberMe, Input.RememberMachine);
            // Not: SignInManager.PasswordSignInAsync içindeki rememberMe (kalýcý cookie için)
            // ve TwoFactorAuthenticatorSignInAsync içindeki rememberMe (2FA cookie'si için) farklý amaçlara hizmet eder.
            // Login sayfasýndaki RememberMe, PasswordSignInAsync'in isPersistent parametresidir.
            // Buradaki RememberMe, 2FA cookie'sinin kalýcý olup olmayacaðýný belirler.
            // Genellikle Login sayfasýndaki "Beni Hatýrla" seçeneði, hem ana oturum hem de 2FA oturumu için geçerli olmalýdýr.

            if (result.Succeeded)
            {
                _logger.LogInformation("Kullanýcý ({UserId}) 2FA ile baþarýyla giriþ yaptý.", user.Id);
                return LocalRedirect(ReturnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("Kullanýcý ({UserId}) 2FA denemelerinde kilitlendi.", user.Id);
                // Eðer bir Lockout sayfanýz varsa ona yönlendirin.
                // return RedirectToPage("./Lockout"); 
                ModelState.AddModelError(string.Empty, "Çok fazla baþarýsýz deneme nedeniyle hesap kilitlendi. Lütfen daha sonra tekrar deneyin.");
                return Page();
            }
            else if (result.IsNotAllowed) // Nadir, genellikle e-posta onayý eksikse vb.
            {
                _logger.LogWarning("Kullanýcý ({UserId}) için 2FA'ya izin verilmedi (IsNotAllowed). E-posta onayý eksik olabilir.", user.Id);
                ModelState.AddModelError(string.Empty, "Bu hesapla giriþ yapmanýza izin verilmiyor. E-posta adresinizin onaylý olduðundan emin olun.");
                return Page();
            }
            else // Geçersiz kod
            {
                _logger.LogWarning("Kullanýcý ({UserId}) için geçersiz 2FA kodu girildi.", user.Id);
                ModelState.AddModelError("Input.TwoFactorCode", "Geçersiz doðrulama kodu. Lütfen tekrar deneyin.");
                return Page();
            }
        }
    }
}