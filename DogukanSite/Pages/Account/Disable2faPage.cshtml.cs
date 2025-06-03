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
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                // E�er 2FA zaten etkin de�ilse, bir mesajla ana 2FA sayfas�na y�nlendir.
                StatusMessage = "Hata: �ki a�amal� do�rulama zaten etkin de�il.";
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded)
            {
                StatusMessage = "Hata: �ki a�amal� do�rulama devre d��� b�rak�l�rken beklenmedik bir hata olu�tu.";
                _logger.LogError("Kullan�c� ({UserId}) i�in 2FA devre d��� b�rak�lamad�: {Errors}", user.Id, string.Join(", ", disable2faResult.Errors.Select(e => e.Description)));
                return RedirectToPage("./TwoFactorAuthenticationPage"); // Hata mesaj�yla ana 2FA sayfas�na
            }

            _logger.LogInformation("Kullan�c� ({UserId}) iki a�amal� do�rulamay� devre d��� b�rakt�.", user.Id);
            StatusMessage = "�ki a�amal� do�rulama ba�ar�yla devre d��� b�rak�ld�.";
            return RedirectToPage("./TwoFactorAuthenticationPage"); // Ba�ar� mesaj�yla ana 2FA sayfas�na
        }
    }
}