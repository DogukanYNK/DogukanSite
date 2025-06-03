// Pages/Account/LoginWithRecoveryCodePage.cshtml.cs
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [AllowAnonymous]
    public class LoginWithRecoveryCodePageModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager; // Opsiyonel, loglama veya kullan�c� bilgisi i�in
        private readonly ILogger<LoginWithRecoveryCodePageModel> _logger;

        public LoginWithRecoveryCodePageModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginWithRecoveryCodePageModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [BindProperty]
            [Required(ErrorMessage = "Kurtarma kodu alan� zorunludur.")]
            [DataType(DataType.Text)]
            [Display(Name = "Kurtarma Kodu")]
            public string RecoveryCode { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            // Kullan�c�n�n �ifre ad�m�n� ge�ti�inden emin ol (2FA kullan�c�s� var m� diye bakar)
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("LoginWithRecoveryCodePage.OnGetAsync: �ki a�amal� do�rulama i�in kullan�c� bulunamad�. Muhtemelen �ifre ad�m� atland�.");
                ErrorMessage = "Kurtarma kodu ile giri� yapabilmek i�in l�tfen �nce e-posta ve �ifrenizle giri� yap�n.";
                return RedirectToPage("./Login");
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            Input = new InputModel(); // Formu bo� ba�lat

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("LoginWithRecoveryCodePage.OnPostAsync: �ki a�amal� do�rulama i�in kullan�c� bulunamad� (POST).");
                ErrorMessage = "�ki a�amal� do�rulama oturumunuz zaman a��m�na u�ram�� olabilir. L�tfen tekrar giri� yapmay� deneyin.";
                return RedirectToPage("./Login", new { ReturnUrl = ReturnUrl });
            }

            var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty); // Bo�luklar� temizle

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("Kullan�c� ({UserId}) kurtarma kodu ile ba�ar�yla giri� yapt�.", user.Id);
                return LocalRedirect(ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Kullan�c� ({UserId}) kurtarma kodu denemelerinde kilitlendi.", user.Id);
                // Lockout sayfan�z varsa ona y�nlendirin
                // return RedirectToPage("./Lockout"); 
                ModelState.AddModelError(string.Empty, "�ok fazla ba�ar�s�z deneme nedeniyle hesap kilitlendi. L�tfen daha sonra tekrar deneyin.");
                return Page();
            }
            else // Ge�ersiz kurtarma kodu
            {
                _logger.LogWarning("Kullan�c� ({UserId}) i�in ge�ersiz kurtarma kodu girildi: {RecoveryCode}", user.Id, recoveryCode);
                ModelState.AddModelError("Input.RecoveryCode", "Ge�ersiz kurtarma kodu girildi.");
                return Page();
            }
        }
    }
}