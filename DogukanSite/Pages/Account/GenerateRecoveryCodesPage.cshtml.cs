// Pages/Account/GenerateRecoveryCodesPage.cshtml.cs
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Linq; // ToArray() için
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class GenerateRecoveryCodesPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GenerateRecoveryCodesPageModel> _logger;

        public GenerateRecoveryCodesPageModel(
            UserManager<ApplicationUser> userManager,
            ILogger<GenerateRecoveryCodesPageModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        // Bu property'ler OnGet'te kullanýcýyý bilgilendirmek için set edilebilir.
        public bool Is2faEnabled { get; set; }
        public int CurrentRecoveryCodesLeft { get; set; }


        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!Is2faEnabled)
            {
                StatusMessage = "Hata: Ýki aþamalý doðrulama etkin deðil. Önce 2FA'yý etkinleþtirmeniz gerekir.";
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }
            CurrentRecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!is2faEnabled)
            {
                StatusMessage = "Hata: Ýki aþamalý doðrulama etkin deðil. Kurtarma kodu üretilemez.";
                _logger.LogWarning("Kullanýcý ({UserId}) 2FA etkin deðilken kurtarma kodu üretmeye çalýþtý.", user.Id);
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }

            // Yeni kurtarma kodlarý üret (genellikle 10 adet)
            // Bu iþlem ayný zamanda mevcut tüm eski kurtarma kodlarýný geçersiz kýlar.
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            if (recoveryCodes == null || !recoveryCodes.Any())
            {
                StatusMessage = "Hata: Yeni kurtarma kodlarý üretilirken bir sorun oluþtu.";
                _logger.LogError("Kullanýcý ({UserId}) için yeni 2FA kurtarma kodlarý üretilemedi.", user.Id);
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }

            _logger.LogInformation("Kullanýcý ({UserId}) için yeni 2FA kurtarma kodlarý üretildi.", user.Id);

            // Yeni kodlarý TempData ile TwoFactorAuthenticationPage'e taþýyýp orada gösterelim.
            TempData["RecoveryCodes"] = recoveryCodes.ToArray();
            TempData["RecoveryCodesGenerated"] = true; // Bayrak: Kodlarýn yeni üretildiðini belirtir
            StatusMessage = "Yeni kurtarma kodlarýnýz baþarýyla üretildi. Lütfen bu kodlarý güvenli bir yerde saklayýn. Her biri sadece bir kez kullanýlabilir.";

            return RedirectToPage("./TwoFactorAuthenticationPage");
        }
    }
}