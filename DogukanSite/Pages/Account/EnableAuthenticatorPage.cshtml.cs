// Pages/Account/EnableAuthenticatorPage.cshtml.cs
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web; // UrlEncoder için
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class EnableAuthenticatorPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EnableAuthenticatorPageModel> _logger;
        private readonly UrlEncoder _urlEncoder; // QR Kod URI'si için

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public EnableAuthenticatorPageModel(
            UserManager<ApplicationUser> userManager,
            ILogger<EnableAuthenticatorPageModel> logger,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        public string SharedKey { get; set; } // Kullanýcýya gösterilecek formatlanmýþ anahtar
        public string AuthenticatorUri { get; set; } // QR Kod için URI

        [TempData]
        public string[] RecoveryCodes { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Doðrulama kodu zorunludur.")]
            [StringLength(7, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunluðunda olmalýdýr.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Doðrulama Kodu")]
            public string VerificationCode { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            await LoadSharedKeyAndQrCodeUriAsync(user);

            // Eðer kullanýcý zaten 2FA etkinleþtirmiþse ve buraya bir þekilde geldiyse, ana 2FA sayfasýna yönlendir
            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                _logger.LogInformation("Kullanýcý ({UserId}) zaten 2FA etkinleþtirmiþ, TwoFactorAuthenticationPage'e yönlendiriliyor.", user.Id);
                StatusMessage = "Ýki aþamalý doðrulama zaten etkin durumda.";
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

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            // Boþluklarý temizle
            var verificationCode = Input.VerificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Input.VerificationCode", "Doðrulama kodu geçersiz veya süresi dolmuþ.");
                _logger.LogWarning("Kullanýcý ({UserId}) için 2FA doðrulama kodu geçersiz.", user.Id);
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var userId = await _userManager.GetUserIdAsync(user);
            _logger.LogInformation("Kullanýcý ({UserId}) ID'li kullanýcý 2FA'yý doðrulama uygulamasý ile etkinleþtirdi.", userId);

            StatusMessage = "Doðrulama uygulamanýz baþarýyla ayarlandý ve 2FA etkinleþtirildi.";

            if (await _userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                RecoveryCodes = recoveryCodes.ToArray(); // TempData ile diðer sayfaya taþýnacak
                TempData["RecoveryCodesGenerated"] = true; // Bayrak
                _logger.LogInformation("Kullanýcý ({UserId}) için yeni kurtarma kodlarý üretildi.", userId);
                // Kurtarma kodlarýný göstermek için direkt TwoFactorAuthenticationPage'e yönlendiriyoruz.
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }

            return RedirectToPage("./TwoFactorAuthenticationPage");
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
        {
            // Authenticator anahtarýný yükle veya oluþtur
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
                _logger.LogInformation("Kullanýcý ({UserId}) için yeni authenticator key oluþturuldu.", user.Id);
            }

            SharedKey = FormatKey(unformattedKey); // Kullanýcýya gösterilecek formatlanmýþ anahtar

            var email = await _userManager.GetEmailAsync(user);
            AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition));
            }
            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            // Uygulama adýnýzý buraya girin (QR kodda görünecek)
            string issuer = "DogukanStore";
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode(issuer),
                _urlEncoder.Encode(email),
                unformattedKey);
        }
    }
}