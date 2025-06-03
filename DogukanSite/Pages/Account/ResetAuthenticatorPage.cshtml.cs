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
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            // E�er kullan�c� zaten bir authenticator ayarlamam��sa veya 2FA etkin de�ilse,
            // bu sayfada yapacak bir �ey yok. Ana 2FA sayfas�na y�nlendir.
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

            if (string.IsNullOrEmpty(unformattedKey) && !isTwoFactorEnabled)
            {
                StatusMessage = "Hata: Hen�z bir do�rulama uygulamas� ayarlanmam�� veya 2FA etkin de�il. S�f�rlanacak bir ayar bulunmuyor.";
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

            // Authenticator anahtar�n� s�f�rla (bu i�lem 2FA'y� da devre d��� b�rak�r gibi davran�r)
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation("Kullan�c� ({UserId}) authenticator anahtar�n� s�f�rlad�.", user.Id);

            // 2FA'y� da explicit olarak devre d��� b�rakal�m (ResetAuthenticatorKeyAsync bazen bunu yapmaz)
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            _logger.LogInformation("Kullan�c� ({UserId}) i�in 2FA, authenticator s�f�rlamas� sonras� devre d��� b�rak�ld�.", user.Id);


            // Kullan�c�n�n oturumunu yenileyerek 2FA durumundaki de�i�ikli�i yans�t
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Do�rulama uygulamas� ayarlar�n�z s�f�rland�. �ki a�amal� do�rulamay� tekrar kullanmak isterseniz, yeni bir do�rulama uygulamas� kurman�z gerekecektir.";

            // Kullan�c�y� yeni bir authenticator kurmaya te�vik etmek i�in EnableAuthenticatorPage'e y�nlendir.
            return RedirectToPage("./EnableAuthenticatorPage");
        }
    }
}