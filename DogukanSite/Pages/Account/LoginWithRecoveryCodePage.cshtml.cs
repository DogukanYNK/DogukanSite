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
        private readonly UserManager<ApplicationUser> _userManager; // Opsiyonel, loglama veya kullanýcý bilgisi için
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
            [Required(ErrorMessage = "Kurtarma kodu alaný zorunludur.")]
            [DataType(DataType.Text)]
            [Display(Name = "Kurtarma Kodu")]
            public string RecoveryCode { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            // Kullanýcýnýn þifre adýmýný geçtiðinden emin ol (2FA kullanýcýsý var mý diye bakar)
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("LoginWithRecoveryCodePage.OnGetAsync: Ýki aþamalý doðrulama için kullanýcý bulunamadý. Muhtemelen þifre adýmý atlandý.");
                ErrorMessage = "Kurtarma kodu ile giriþ yapabilmek için lütfen önce e-posta ve þifrenizle giriþ yapýn.";
                return RedirectToPage("./Login");
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            Input = new InputModel(); // Formu boþ baþlat

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
                _logger.LogWarning("LoginWithRecoveryCodePage.OnPostAsync: Ýki aþamalý doðrulama için kullanýcý bulunamadý (POST).");
                ErrorMessage = "Ýki aþamalý doðrulama oturumunuz zaman aþýmýna uðramýþ olabilir. Lütfen tekrar giriþ yapmayý deneyin.";
                return RedirectToPage("./Login", new { ReturnUrl = ReturnUrl });
            }

            var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty); // Boþluklarý temizle

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("Kullanýcý ({UserId}) kurtarma kodu ile baþarýyla giriþ yaptý.", user.Id);
                return LocalRedirect(ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Kullanýcý ({UserId}) kurtarma kodu denemelerinde kilitlendi.", user.Id);
                // Lockout sayfanýz varsa ona yönlendirin
                // return RedirectToPage("./Lockout"); 
                ModelState.AddModelError(string.Empty, "Çok fazla baþarýsýz deneme nedeniyle hesap kilitlendi. Lütfen daha sonra tekrar deneyin.");
                return Page();
            }
            else // Geçersiz kurtarma kodu
            {
                _logger.LogWarning("Kullanýcý ({UserId}) için geçersiz kurtarma kodu girildi: {RecoveryCode}", user.Id, recoveryCode);
                ModelState.AddModelError("Input.RecoveryCode", "Geçersiz kurtarma kodu girildi.");
                return Page();
            }
        }
    }
}