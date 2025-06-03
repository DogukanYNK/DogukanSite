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
    [AllowAnonymous] // Bu sayfa, kullan�c� hen�z tam olarak giri� yapmad��� i�in anonim eri�ime izin vermelidir.
    public class LoginWith2faPageModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager; // Hata mesaj�nda kullan�c� ad� g�stermek i�in (opsiyonel)
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

        public bool RememberMe { get; set; } // Login sayfas�ndan gelen "Beni Hat�rla" durumu

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Do�rulama kodu zorunludur.")]
            [StringLength(7, MinimumLength = 6, ErrorMessage = "{0} 6 ile 7 karakter aras�nda olmal�d�r.")]
            [DataType(DataType.Text)]
            [Display(Name = "Do�rulama Kodu")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Bu makineyi hat�rla")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            // Kullan�c�n�n �ifre ad�m�n� ge�ti�inden emin ol
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                // Bu durum, kullan�c�n�n do�rudan bu sayfaya gelmeye �al��t��� anlam�na gelir.
                // Ana giri� sayfas�na y�nlendirmek daha mant�kl�.
                _logger.LogWarning("LoginWith2faPage.OnGetAsync: �ki a�amal� do�rulama i�in kullan�c� bulunamad�. Muhtemelen �ifre ad�m� atland�.");
                ErrorMessage = "�ki a�amal� do�rulama i�in ge�erli bir kullan�c� oturumu bulunamad�. L�tfen tekrar giri� yapmay� deneyin.";
                return RedirectToPage("./Login");
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            RememberMe = rememberMe;
            Input = new InputModel(); // Formu bo� ba�lat

            _logger.LogInformation("Kullan�c� ({UserId}) i�in LoginWith2faPage y�klendi.", user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            RememberMe = rememberMe; // Login sayfas�ndan gelen de�eri koru

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("LoginWith2faPage.OnPostAsync: �ki a�amal� do�rulama i�in kullan�c� bulunamad� (POST).");
                ErrorMessage = "�ki a�amal� do�rulama oturumunuz zaman a��m�na u�ram�� olabilir. L�tfen tekrar giri� yapmay� deneyin.";
                // Burada direkt RedirectToPage("./Login") yerine ErrorMessage ile Page() d�nd�rmek,
                // kullan�c�n�n girdi�i formu kaybetmemesini sa�lar (e�er bir hata varsa).
                // Ancak session/cookie temelli 2FA kullan�c�s� i�in bu, genellikle Login'e y�nlendirmeyi gerektirir.
                return RedirectToPage("./Login", new { ReturnUrl = ReturnUrl });
            }

            var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, RememberMe, Input.RememberMachine);
            // Not: SignInManager.PasswordSignInAsync i�indeki rememberMe (kal�c� cookie i�in)
            // ve TwoFactorAuthenticatorSignInAsync i�indeki rememberMe (2FA cookie'si i�in) farkl� ama�lara hizmet eder.
            // Login sayfas�ndaki RememberMe, PasswordSignInAsync'in isPersistent parametresidir.
            // Buradaki RememberMe, 2FA cookie'sinin kal�c� olup olmayaca��n� belirler.
            // Genellikle Login sayfas�ndaki "Beni Hat�rla" se�ene�i, hem ana oturum hem de 2FA oturumu i�in ge�erli olmal�d�r.

            if (result.Succeeded)
            {
                _logger.LogInformation("Kullan�c� ({UserId}) 2FA ile ba�ar�yla giri� yapt�.", user.Id);
                return LocalRedirect(ReturnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("Kullan�c� ({UserId}) 2FA denemelerinde kilitlendi.", user.Id);
                // E�er bir Lockout sayfan�z varsa ona y�nlendirin.
                // return RedirectToPage("./Lockout"); 
                ModelState.AddModelError(string.Empty, "�ok fazla ba�ar�s�z deneme nedeniyle hesap kilitlendi. L�tfen daha sonra tekrar deneyin.");
                return Page();
            }
            else if (result.IsNotAllowed) // Nadir, genellikle e-posta onay� eksikse vb.
            {
                _logger.LogWarning("Kullan�c� ({UserId}) i�in 2FA'ya izin verilmedi (IsNotAllowed). E-posta onay� eksik olabilir.", user.Id);
                ModelState.AddModelError(string.Empty, "Bu hesapla giri� yapman�za izin verilmiyor. E-posta adresinizin onayl� oldu�undan emin olun.");
                return Page();
            }
            else // Ge�ersiz kod
            {
                _logger.LogWarning("Kullan�c� ({UserId}) i�in ge�ersiz 2FA kodu girildi.", user.Id);
                ModelState.AddModelError("Input.TwoFactorCode", "Ge�ersiz do�rulama kodu. L�tfen tekrar deneyin.");
                return Page();
            }
        }
    }
}