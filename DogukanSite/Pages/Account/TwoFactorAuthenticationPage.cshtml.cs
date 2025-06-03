// Pages/Account/TwoFactorAuthenticationPage.cshtml.cs
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class TwoFactorAuthenticationPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<TwoFactorAuthenticationPageModel> _logger;

        public TwoFactorAuthenticationPageModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<TwoFactorAuthenticationPageModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public bool HasAuthenticator { get; set; } // Authenticator uygulamasý kurulmuþ mu?
        public bool Is2faEnabled { get; set; }
        public bool IsMachineRemembered { get; set; } // Bu tarayýcý 2FA için hatýrlanýyor mu?
        public int RecoveryCodesLeft { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string[] RecoveryCodes { get; set; } // Yeni üretilen kurtarma kodlarýný göstermek için

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

            // HasAuthenticator, genellikle kullanýcýnýn bir authenticator key'i olup olmadýðýna bakar.
            // Identity'nin varsayýlan akýþýnda, authenticator key ayarlanýr ve sonra 2FA etkinleþtirilir.
            var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
            HasAuthenticator = !string.IsNullOrEmpty(authenticatorKey);


            // Eðer TempData'dan gelen kurtarma kodlarý varsa, RecoveryCodesLeft'i ona göre ayarla
            // (çünkü yeni kodlar üretildiðinde eski kodlar geçersiz olur).
            if (TempData["RecoveryCodesGenerated"] != null && (bool)TempData["RecoveryCodesGenerated"] == true && TempData["RecoveryCodes"] != null)
            {
                RecoveryCodesLeft = ((string[])TempData["RecoveryCodes"])?.Length ?? 0;
                // TempData'daki RecoveryCodes zaten view'a aktarýlacak.
            }


            return Page();
        }

        public async Task<IActionResult> OnPostForgetClientAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            await _signInManager.ForgetTwoFactorClientAsync();
            StatusMessage = "Bu tarayýcýnýn iki aþamalý doðrulama için hatýrlanmasý kaldýrýldý. Bir sonraki giriþinizde doðrulama kodu istenecektir.";
            _logger.LogInformation("Kullanýcý ({UserId}) için 2FA tarayýcý hatýrlamasý kaldýrýldý.", user.Id);
            return RedirectToPage();
        }
    }
}