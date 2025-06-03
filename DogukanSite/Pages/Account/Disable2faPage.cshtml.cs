// Pages/Account/Disable2faPage.cshtml.cs
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
    public class Disable2faPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<Disable2faPageModel> _logger;

        public Disable2faPageModel(
            UserManager<ApplicationUser> userManager,
            ILogger<Disable2faPageModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                // Eðer 2FA zaten etkin deðilse, bir mesajla ana 2FA sayfasýna yönlendir.
                StatusMessage = "Hata: Ýki aþamalý doðrulama zaten etkin deðil.";
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded)
            {
                StatusMessage = "Hata: Ýki aþamalý doðrulama devre dýþý býrakýlýrken beklenmedik bir hata oluþtu.";
                _logger.LogError("Kullanýcý ({UserId}) için 2FA devre dýþý býrakýlamadý: {Errors}", user.Id, string.Join(", ", disable2faResult.Errors.Select(e => e.Description)));
                return RedirectToPage("./TwoFactorAuthenticationPage"); // Hata mesajýyla ana 2FA sayfasýna
            }

            _logger.LogInformation("Kullanýcý ({UserId}) iki aþamalý doðrulamayý devre dýþý býraktý.", user.Id);
            StatusMessage = "Ýki aþamalý doðrulama baþarýyla devre dýþý býrakýldý.";
            return RedirectToPage("./TwoFactorAuthenticationPage"); // Baþarý mesajýyla ana 2FA sayfasýna
        }
    }
}