// Pages/Account/ConfirmEmailPage.cshtml.cs
using DogukanSite.Models; // ApplicationUser i�in
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities; // WebEncoders i�in
using Microsoft.Extensions.Logging;
using System.Text; // Encoding i�in
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [AllowAnonymous] // Bu sayfa, e-posta linkine t�klayan herkes taraf�ndan eri�ilebilir olmal�
    public class ConfirmEmailPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfirmEmailPageModel> _logger;

        public ConfirmEmailPageModel(UserManager<ApplicationUser> userManager, ILogger<ConfirmEmailPageModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            ViewData["Title"] = "E-posta Onay�";

            if (userId == null || code == null)
            {
                _logger.LogWarning("ConfirmEmailPage.OnGetAsync: userId veya code null geldi. Ana sayfaya y�nlendiriliyor.");
                return RedirectToPage("/Index"); // Veya bir hata mesaj� ile Login sayfas�na
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmEmailPage.OnGetAsync: Kullan�c� ID '{UserId}' ile bulunamad�.", userId);
                StatusMessage = "Hata: E-posta adresiniz onaylan�rken bir sorun olu�tu (Kullan�c� bulunamad�).";
                return Page();
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "ConfirmEmailPage.OnGetAsync: Ge�ersiz Base64Url kod format�. Code: {Code}", code);
                StatusMessage = "Hata: E-posta onaylama kodu ge�ersiz.";
                return Page();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                StatusMessage = "E-posta adresiniz ba�ar�yla onayland�. Art�k giri� yapabilirsiniz.";
                _logger.LogInformation("Kullan�c� ({UserId}) e-posta adresini ba�ar�yla onaylad�.", userId);
            }
            else
            {
                StatusMessage = "Hata: E-posta adresiniz onaylanamad�.";
                _logger.LogError("Kullan�c� ({UserId}) e-posta adresini onaylayamad�. Hatalar: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return Page();
        }
    }
}