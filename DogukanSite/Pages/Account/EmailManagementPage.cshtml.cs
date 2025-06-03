// Pages/Account/EmailManagementPage.cshtml.cs
using DogukanSite.Models;
using DogukanSite.Services; // IEmailSender i�in (kendi namespace'inize g�re ayarlay�n)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities; // QueryHelpers i�in
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web; // HtmlEncoder i�in
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class EmailManagementPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Services.IEmailSender _emailSender;
        private readonly ILogger<EmailManagementPageModel> _logger;

        public EmailManagementPageModel(
            UserManager<ApplicationUser> userManager,
            DogukanSite.Services.IEmailSender emailSender, // Tam ad alan� kullan�ld�
            ILogger<EmailManagementPageModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender; // Atama ayn� kal�r
            _logger = logger;
        }

        public string CurrentEmail { get; set; }
        public bool IsEmailConfirmed { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Yeni e-posta alan� zorunludur.")]
            [EmailAddress(ErrorMessage = "Ge�erli bir e-posta adresi giriniz.")]
            [Display(Name = "Yeni E-posta Adresiniz")]
            public string NewEmail { get; set; }
        }

        private async Task LoadUserDataAsync(ApplicationUser user)
        {
            CurrentEmail = await _userManager.GetEmailAsync(user);
            Input = new InputModel
            {
                NewEmail = CurrentEmail, // Formu mevcut e-posta ile doldur
            };
            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }
            await LoadUserDataAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            if (!ModelState.IsValid)
            {
                await LoadUserDataAsync(user);
                return Page();
            }

            var currentEmail = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != null && Input.NewEmail.Equals(currentEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Girdi�iniz e-posta adresi mevcut adresinizle ayn�.";
                return RedirectToPage();
            }

            // Yeni e-posta i�in onay token'� olu�tur
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code)); // Kodu URL uyumlu yap

            // ONAY SAYFASININ ADINI VE YOLUNU KEND� PROJEN�ZE G�RE AYARLAYIN
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailChangePage", // Bu isimde bir sayfa olu�turman�z gerekecek
                pageHandler: null,
                values: new { userId = userId, email = Input.NewEmail, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.NewEmail,
                "E-posta Adresinizi De�i�tirmeyi Onaylay�n - DogukanStore",
                $"L�tfen DogukanStore hesab�n�zla ili�kili e-posta adresinizi <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>buraya t�klayarak</a> onaylay�n.");

            StatusMessage = "E-posta adresinizi de�i�tirmek i�in onay linki yeni e-posta adresinize g�nderildi. L�tfen e-postan�z� kontrol edin.";
            _logger.LogInformation("Kullan�c� ({UserId}) i�in e-posta de�i�tirme onay� yeni adrese ({NewEmail}) g�nderildi.", userId, Input.NewEmail);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                StatusMessage = "E-posta adresiniz zaten onaylanm�� durumda.";
                return RedirectToPage();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // ONAY SAYFASININ ADINI VE YOLUNU KEND� PROJEN�ZE G�RE AYARLAYIN
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailPage", // Bu isimde bir sayfa olu�turman�z gerekecek
                pageHandler: null,
                values: new { userId = userId, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "E-posta Adresinizi Onaylay�n - DogukanStore",
                $"L�tfen DogukanStore hesab�n�z� <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>buraya t�klayarak</a> onaylay�n.");

            StatusMessage = "Do�rulama e-postas� g�nderildi. L�tfen e-posta kutunuzu kontrol edin.";
            _logger.LogInformation("Kullan�c� ({UserId}) i�in e-posta ({Email}) do�rulama maili tekrar g�nderildi.", userId, email);
            return RedirectToPage();
        }
    }
}