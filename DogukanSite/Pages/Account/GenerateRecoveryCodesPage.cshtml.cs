// Pages/Account/GenerateRecoveryCodesPage.cshtml.cs
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Linq; // ToArray() i�in
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

        // Bu property'ler OnGet'te kullan�c�y� bilgilendirmek i�in set edilebilir.
        public bool Is2faEnabled { get; set; }
        public int CurrentRecoveryCodesLeft { get; set; }


        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!Is2faEnabled)
            {
                StatusMessage = "Hata: �ki a�amal� do�rulama etkin de�il. �nce 2FA'y� etkinle�tirmeniz gerekir.";
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
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!is2faEnabled)
            {
                StatusMessage = "Hata: �ki a�amal� do�rulama etkin de�il. Kurtarma kodu �retilemez.";
                _logger.LogWarning("Kullan�c� ({UserId}) 2FA etkin de�ilken kurtarma kodu �retmeye �al��t�.", user.Id);
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }

            // Yeni kurtarma kodlar� �ret (genellikle 10 adet)
            // Bu i�lem ayn� zamanda mevcut t�m eski kurtarma kodlar�n� ge�ersiz k�lar.
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            if (recoveryCodes == null || !recoveryCodes.Any())
            {
                StatusMessage = "Hata: Yeni kurtarma kodlar� �retilirken bir sorun olu�tu.";
                _logger.LogError("Kullan�c� ({UserId}) i�in yeni 2FA kurtarma kodlar� �retilemedi.", user.Id);
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }

            _logger.LogInformation("Kullan�c� ({UserId}) i�in yeni 2FA kurtarma kodlar� �retildi.", user.Id);

            // Yeni kodlar� TempData ile TwoFactorAuthenticationPage'e ta��y�p orada g�sterelim.
            TempData["RecoveryCodes"] = recoveryCodes.ToArray();
            TempData["RecoveryCodesGenerated"] = true; // Bayrak: Kodlar�n yeni �retildi�ini belirtir
            StatusMessage = "Yeni kurtarma kodlar�n�z ba�ar�yla �retildi. L�tfen bu kodlar� g�venli bir yerde saklay�n. Her biri sadece bir kez kullan�labilir.";

            return RedirectToPage("./TwoFactorAuthenticationPage");
        }
    }
}