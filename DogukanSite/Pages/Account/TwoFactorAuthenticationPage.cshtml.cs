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

        public bool HasAuthenticator { get; set; } // Authenticator uygulamas� kurulmu� mu?
        public bool Is2faEnabled { get; set; }
        public bool IsMachineRemembered { get; set; } // Bu taray�c� 2FA i�in hat�rlan�yor mu?
        public int RecoveryCodesLeft { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string[] RecoveryCodes { get; set; } // Yeni �retilen kurtarma kodlar�n� g�stermek i�in

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

            // HasAuthenticator, genellikle kullan�c�n�n bir authenticator key'i olup olmad���na bakar.
            // Identity'nin varsay�lan ak���nda, authenticator key ayarlan�r ve sonra 2FA etkinle�tirilir.
            var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
            HasAuthenticator = !string.IsNullOrEmpty(authenticatorKey);


            // E�er TempData'dan gelen kurtarma kodlar� varsa, RecoveryCodesLeft'i ona g�re ayarla
            // (��nk� yeni kodlar �retildi�inde eski kodlar ge�ersiz olur).
            if (TempData["RecoveryCodesGenerated"] != null && (bool)TempData["RecoveryCodesGenerated"] == true && TempData["RecoveryCodes"] != null)
            {
                RecoveryCodesLeft = ((string[])TempData["RecoveryCodes"])?.Length ?? 0;
                // TempData'daki RecoveryCodes zaten view'a aktar�lacak.
            }


            return Page();
        }

        public async Task<IActionResult> OnPostForgetClientAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            await _signInManager.ForgetTwoFactorClientAsync();
            StatusMessage = "Bu taray�c�n�n iki a�amal� do�rulama i�in hat�rlanmas� kald�r�ld�. Bir sonraki giri�inizde do�rulama kodu istenecektir.";
            _logger.LogInformation("Kullan�c� ({UserId}) i�in 2FA taray�c� hat�rlamas� kald�r�ld�.", user.Id);
            return RedirectToPage();
        }
    }
}