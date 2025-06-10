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

        public bool HasAuthenticator { get; set; }
        public bool Is2faEnabled { get; set; }
        public bool IsMachineRemembered { get; set; }
        public int RecoveryCodesLeft { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        // ----- EKS�K OLAN VE ��MD� EKLENEN SATIR BURASI -----
        [TempData]
        public string[] RecoveryCodes { get; set; }

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
            HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;

            ViewData["Title"] = "�ki A�amal� Do�rulama (2FA)";
            ViewData["ActivePage"] = "Security";
            ViewData["ActiveSecurityPage"] = "TwoFactorAuth";

            return Page();
        }
        public async Task<IActionResult> OnPostEnable2faAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            // Sadece 2FA'y� etkinle�tir, ��nk� kurulum zaten mevcut.
            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!result.Succeeded)
            {
                StatusMessage = "Hata: �ki a�amal� do�rulama etkinle�tirilirken bir sorun olu�tu.";
                return RedirectToPage();
            }

            _logger.LogInformation("Kullan�c� ({UserId}) 2FA'y� yeniden etkinle�tirdi.", user.Id);
            StatusMessage = "�ki a�amal� do�rulama yeniden etkinle�tirildi.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostForgetClient()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            await _signInManager.ForgetTwoFactorClientAsync();
            StatusMessage = "Bu taray�c�n�n iki a�amal� do�rulama i�in hat�rlanmas� kald�r�ld�.";
            _logger.LogInformation("Kullan�c� ({UserId}) i�in 2FA taray�c� hat�rlamas� kald�r�ld�.", user.Id);
            return RedirectToPage();
        }
    }
}