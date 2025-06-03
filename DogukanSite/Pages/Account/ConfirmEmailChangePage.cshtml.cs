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
    [AllowAnonymous] // E-posta linkinden gelineceði için anonim eriþime izin ver
    public class ConfirmEmailChangePageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // E-posta deðiþince oturumu yenilemek için
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
            ViewData["Title"] = "E-posta Deðiþikliði Onayý";

            if (userId == null || email == null || code == null)
            {
                _logger.LogWarning("ConfirmEmailChangePage.OnGetAsync: userId, email veya code null geldi. Ana sayfaya yönlendiriliyor.");
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmEmailChangePage.OnGetAsync: Kullanýcý ID '{UserId}' ile bulunamadý.", userId);
                StatusMessage = $"Hata: E-posta adresiniz deðiþtirilirken bir sorun oluþtu (Kullanýcý bulunamadý).";
                return Page();
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "ConfirmEmailChangePage.OnGetAsync: Geçersiz Base64Url kod formatý. Code: {Code}", code);
                StatusMessage = "Hata: E-posta deðiþtirme kodu geçersiz.";
                return Page();
            }

            // E-postayý ve kullanýcý adýný (eðer e-posta ile aynýysa) deðiþtir
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                StatusMessage = "Hata: E-posta adresiniz deðiþtirilemedi.";
                _logger.LogError("Kullanýcý ({UserId}) için e-posta ({NewEmail}) deðiþtirilemedi. Hatalar: {Errors}", userId, email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Page();
            }

            // Eðer kullanýcý adýnýz e-posta ile aynýysa, onu da güncellemeniz gerekebilir.
            // Bu, uygulamanýzýn mantýðýna baðlýdýr.
            var currentUserName = await _userManager.GetUserNameAsync(user);
            if (currentUserName != null && currentUserName.Contains("@") && !currentUserName.Equals(email, System.StringComparison.OrdinalIgnoreCase)) // Örnek bir kontrol
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!setUserNameResult.Succeeded)
                {
                    StatusMessage = $"E-posta adresiniz baþarýyla deðiþtirildi ancak kullanýcý adýnýz güncellenirken bir sorun oluþtu: {string.Join(", ", setUserNameResult.Errors.Select(e => e.Description))}";
                    _logger.LogError("Kullanýcý ({UserId}) için e-posta deðiþtirildi ama kullanýcý adý ({NewEmail}) güncellenemedi.", userId, email);
                    // Oturumu yine de yenile, çünkü e-posta deðiþti.
                    await _signInManager.RefreshSignInAsync(user);
                    return Page();
                }
                _logger.LogInformation("Kullanýcý ({UserId}) için kullanýcý adý da yeni e-posta ({NewEmail}) olarak güncellendi.", userId, email);
            }

            // Kullanýcýnýn oturumunu yenileyerek yeni e-posta adresinin ve ilgili claim'lerin geçerli olmasýný saðla.
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "E-posta adresiniz baþarýyla deðiþtirildi ve onaylandý.";
            _logger.LogInformation("Kullanýcý ({UserId}) e-posta adresini baþarýyla ({NewEmail}) olarak deðiþtirdi ve onayladý.", userId, email);
            return Page();
        }
    }
}