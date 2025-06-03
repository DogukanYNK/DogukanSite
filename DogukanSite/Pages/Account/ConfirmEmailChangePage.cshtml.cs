// Pages/Account/ConfirmEmailChangePage.cshtml.cs
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [AllowAnonymous] // E-posta linkinden gelinece�i i�in anonim eri�ime izin ver
    public class ConfirmEmailChangePageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // E-posta de�i�ince oturumu yenilemek i�in
        private readonly ILogger<ConfirmEmailChangePageModel> _logger;

        public ConfirmEmailChangePageModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ConfirmEmailChangePageModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            ViewData["Title"] = "E-posta De�i�ikli�i Onay�";

            if (userId == null || email == null || code == null)
            {
                _logger.LogWarning("ConfirmEmailChangePage.OnGetAsync: userId, email veya code null geldi. Ana sayfaya y�nlendiriliyor.");
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmEmailChangePage.OnGetAsync: Kullan�c� ID '{UserId}' ile bulunamad�.", userId);
                StatusMessage = $"Hata: E-posta adresiniz de�i�tirilirken bir sorun olu�tu (Kullan�c� bulunamad�).";
                return Page();
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "ConfirmEmailChangePage.OnGetAsync: Ge�ersiz Base64Url kod format�. Code: {Code}", code);
                StatusMessage = "Hata: E-posta de�i�tirme kodu ge�ersiz.";
                return Page();
            }

            // E-postay� ve kullan�c� ad�n� (e�er e-posta ile ayn�ysa) de�i�tir
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                StatusMessage = "Hata: E-posta adresiniz de�i�tirilemedi.";
                _logger.LogError("Kullan�c� ({UserId}) i�in e-posta ({NewEmail}) de�i�tirilemedi. Hatalar: {Errors}", userId, email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Page();
            }

            // E�er kullan�c� ad�n�z e-posta ile ayn�ysa, onu da g�ncellemeniz gerekebilir.
            // Bu, uygulaman�z�n mant���na ba�l�d�r.
            var currentUserName = await _userManager.GetUserNameAsync(user);
            if (currentUserName != null && currentUserName.Contains("@") && !currentUserName.Equals(email, System.StringComparison.OrdinalIgnoreCase)) // �rnek bir kontrol
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!setUserNameResult.Succeeded)
                {
                    StatusMessage = $"E-posta adresiniz ba�ar�yla de�i�tirildi ancak kullan�c� ad�n�z g�ncellenirken bir sorun olu�tu: {string.Join(", ", setUserNameResult.Errors.Select(e => e.Description))}";
                    _logger.LogError("Kullan�c� ({UserId}) i�in e-posta de�i�tirildi ama kullan�c� ad� ({NewEmail}) g�ncellenemedi.", userId, email);
                    // Oturumu yine de yenile, ��nk� e-posta de�i�ti.
                    await _signInManager.RefreshSignInAsync(user);
                    return Page();
                }
                _logger.LogInformation("Kullan�c� ({UserId}) i�in kullan�c� ad� da yeni e-posta ({NewEmail}) olarak g�ncellendi.", userId, email);
            }

            // Kullan�c�n�n oturumunu yenileyerek yeni e-posta adresinin ve ilgili claim'lerin ge�erli olmas�n� sa�la.
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "E-posta adresiniz ba�ar�yla de�i�tirildi ve onayland�.";
            _logger.LogInformation("Kullan�c� ({UserId}) e-posta adresini ba�ar�yla ({NewEmail}) olarak de�i�tirdi ve onaylad�.", userId, email);
            return Page();
        }
    }
}