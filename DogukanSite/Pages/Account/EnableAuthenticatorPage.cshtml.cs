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
using System.Text.Encodings.Web; // UrlEncoder i�in
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class EnableAuthenticatorPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EnableAuthenticatorPageModel> _logger;
        private readonly UrlEncoder _urlEncoder; // QR Kod URI'si i�in

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

        public string SharedKey { get; set; } // Kullan�c�ya g�sterilecek formatlanm�� anahtar
        public string AuthenticatorUri { get; set; } // QR Kod i�in URI

        [TempData]
        public string[] RecoveryCodes { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Do�rulama kodu zorunludur.")]
            [StringLength(7, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunlu�unda olmal�d�r.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Do�rulama Kodu")]
            public string VerificationCode { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            await LoadSharedKeyAndQrCodeUriAsync(user);

            // E�er kullan�c� zaten 2FA etkinle�tirmi�se ve buraya bir �ekilde geldiyse, ana 2FA sayfas�na y�nlendir
            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                _logger.LogInformation("Kullan�c� ({UserId}) zaten 2FA etkinle�tirmi�, TwoFactorAuthenticationPage'e y�nlendiriliyor.", user.Id);
                StatusMessage = "�ki a�amal� do�rulama zaten etkin durumda.";
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

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            // Bo�luklar� temizle
            var verificationCode = Input.VerificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Input.VerificationCode", "Do�rulama kodu ge�ersiz veya s�resi dolmu�.");
                _logger.LogWarning("Kullan�c� ({UserId}) i�in 2FA do�rulama kodu ge�ersiz.", user.Id);
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var userId = await _userManager.GetUserIdAsync(user);
            _logger.LogInformation("Kullan�c� ({UserId}) ID'li kullan�c� 2FA'y� do�rulama uygulamas� ile etkinle�tirdi.", userId);

            StatusMessage = "Do�rulama uygulaman�z ba�ar�yla ayarland� ve 2FA etkinle�tirildi.";

            if (await _userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                RecoveryCodes = recoveryCodes.ToArray(); // TempData ile di�er sayfaya ta��nacak
                TempData["RecoveryCodesGenerated"] = true; // Bayrak
                _logger.LogInformation("Kullan�c� ({UserId}) i�in yeni kurtarma kodlar� �retildi.", userId);
                // Kurtarma kodlar�n� g�stermek i�in direkt TwoFactorAuthenticationPage'e y�nlendiriyoruz.
                return RedirectToPage("./TwoFactorAuthenticationPage");
            }

            return RedirectToPage("./TwoFactorAuthenticationPage");
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
        {
            // Authenticator anahtar�n� y�kle veya olu�tur
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
                _logger.LogInformation("Kullan�c� ({UserId}) i�in yeni authenticator key olu�turuldu.", user.Id);
            }

            SharedKey = FormatKey(unformattedKey); // Kullan�c�ya g�sterilecek formatlanm�� anahtar

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
            // Uygulama ad�n�z� buraya girin (QR kodda g�r�necek)
            string issuer = "DogukanStore";
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode(issuer),
                _urlEncoder.Encode(email),
                unformattedKey);
        }
    }
}