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

        // ----- EKSÝK OLAN VE ÞÝMDÝ EKLENEN SATIR BURASI -----
        [TempData]
        public string[] RecoveryCodes { get; set; }

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
            HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;

            ViewData["Title"] = "Ýki Aþamalý Doðrulama (2FA)";
            ViewData["ActivePage"] = "Security";
            ViewData["ActiveSecurityPage"] = "TwoFactorAuth";

            return Page();
        }
        public async Task<IActionResult> OnPostEnable2faAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            // Sadece 2FA'yý etkinleþtir, çünkü kurulum zaten mevcut.
            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!result.Succeeded)
            {
                StatusMessage = "Hata: Ýki aþamalý doðrulama etkinleþtirilirken bir sorun oluþtu.";
                return RedirectToPage();
            }

            _logger.LogInformation("Kullanýcý ({UserId}) 2FA'yý yeniden etkinleþtirdi.", user.Id);
            StatusMessage = "Ýki aþamalý doðrulama yeniden etkinleþtirildi.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostForgetClient()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            await _signInManager.ForgetTwoFactorClientAsync();
            StatusMessage = "Bu tarayýcýnýn iki aþamalý doðrulama için hatýrlanmasý kaldýrýldý.";
            _logger.LogInformation("Kullanýcý ({UserId}) için 2FA tarayýcý hatýrlamasý kaldýrýldý.", user.Id);
            return RedirectToPage();
        }
    }
}