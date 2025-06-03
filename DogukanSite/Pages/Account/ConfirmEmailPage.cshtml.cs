// Pages/Account/ConfirmEmailPage.cshtml.cs
using DogukanSite.Models; // ApplicationUser için
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities; // WebEncoders için
using Microsoft.Extensions.Logging;
using System.Text; // Encoding için
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [AllowAnonymous] // Bu sayfa, e-posta linkine týklayan herkes tarafýndan eriþilebilir olmalý
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
            ViewData["Title"] = "E-posta Onayý";

            if (userId == null || code == null)
            {
                _logger.LogWarning("ConfirmEmailPage.OnGetAsync: userId veya code null geldi. Ana sayfaya yönlendiriliyor.");
                return RedirectToPage("/Index"); // Veya bir hata mesajý ile Login sayfasýna
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmEmailPage.OnGetAsync: Kullanýcý ID '{UserId}' ile bulunamadý.", userId);
                StatusMessage = "Hata: E-posta adresiniz onaylanýrken bir sorun oluþtu (Kullanýcý bulunamadý).";
                return Page();
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "ConfirmEmailPage.OnGetAsync: Geçersiz Base64Url kod formatý. Code: {Code}", code);
                StatusMessage = "Hata: E-posta onaylama kodu geçersiz.";
                return Page();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                StatusMessage = "E-posta adresiniz baþarýyla onaylandý. Artýk giriþ yapabilirsiniz.";
                _logger.LogInformation("Kullanýcý ({UserId}) e-posta adresini baþarýyla onayladý.", userId);
            }
            else
            {
                StatusMessage = "Hata: E-posta adresiniz onaylanamadý.";
                _logger.LogError("Kullanýcý ({UserId}) e-posta adresini onaylayamadý. Hatalar: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return Page();
        }
    }
}