// Pages/Account/ResetAuthenticatorPage.cshtml.cs
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
    public class ResetAuthenticatorPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ResetAuthenticatorPageModel> _logger;

        public ResetAuthenticatorPageModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ResetAuthenticatorPageModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

            // Eðer kullanýcý zaten bir authenticator ayarlamamýþsa veya 2FA etkin deðilse,
            // bu sayfada yapacak bir þey yok. Ana 2FA sayfasýna yönlendir.
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

            if (string.IsNullOrEmpty(unformattedKey) && !isTwoFactorEnabled)
            {
                StatusMessage = "Hata: Henüz bir doðrulama uygulamasý ayarlanmamýþ veya 2FA etkin deðil. Sýfýrlanacak bir ayar bulunmuyor.";
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

            // Authenticator anahtarýný sýfýrla (bu iþlem 2FA'yý da devre dýþý býrakýr gibi davranýr)
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation("Kullanýcý ({UserId}) authenticator anahtarýný sýfýrladý.", user.Id);

            // 2FA'yý da explicit olarak devre dýþý býrakalým (ResetAuthenticatorKeyAsync bazen bunu yapmaz)
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            _logger.LogInformation("Kullanýcý ({UserId}) için 2FA, authenticator sýfýrlamasý sonrasý devre dýþý býrakýldý.", user.Id);


            // Kullanýcýnýn oturumunu yenileyerek 2FA durumundaki deðiþikliði yansýt
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Doðrulama uygulamasý ayarlarýnýz sýfýrlandý. Ýki aþamalý doðrulamayý tekrar kullanmak isterseniz, yeni bir doðrulama uygulamasý kurmanýz gerekecektir.";

            // Kullanýcýyý yeni bir authenticator kurmaya teþvik etmek için EnableAuthenticatorPage'e yönlendir.
            return RedirectToPage("./EnableAuthenticatorPage");
        }
    }
}