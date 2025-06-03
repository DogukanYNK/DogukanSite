// Pages/Account/EmailManagementPage.cshtml.cs
using DogukanSite.Models;
using DogukanSite.Services; // IEmailSender için (kendi namespace'inize göre ayarlayýn)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities; // QueryHelpers için
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web; // HtmlEncoder için
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
            DogukanSite.Services.IEmailSender emailSender, // Tam ad alaný kullanýldý
            ILogger<EmailManagementPageModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender; // Atama ayný kalýr
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
            [Required(ErrorMessage = "Yeni e-posta alaný zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
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
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }
            await LoadUserDataAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            if (!ModelState.IsValid)
            {
                await LoadUserDataAsync(user);
                return Page();
            }

            var currentEmail = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != null && Input.NewEmail.Equals(currentEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Girdiðiniz e-posta adresi mevcut adresinizle ayný.";
                return RedirectToPage();
            }

            // Yeni e-posta için onay token'ý oluþtur
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code)); // Kodu URL uyumlu yap

            // ONAY SAYFASININ ADINI VE YOLUNU KENDÝ PROJENÝZE GÖRE AYARLAYIN
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailChangePage", // Bu isimde bir sayfa oluþturmanýz gerekecek
                pageHandler: null,
                values: new { userId = userId, email = Input.NewEmail, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.NewEmail,
                "E-posta Adresinizi Deðiþtirmeyi Onaylayýn - DogukanStore",
                $"Lütfen DogukanStore hesabýnýzla iliþkili e-posta adresinizi <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>buraya týklayarak</a> onaylayýn.");

            StatusMessage = "E-posta adresinizi deðiþtirmek için onay linki yeni e-posta adresinize gönderildi. Lütfen e-postanýzý kontrol edin.";
            _logger.LogInformation("Kullanýcý ({UserId}) için e-posta deðiþtirme onayý yeni adrese ({NewEmail}) gönderildi.", userId, Input.NewEmail);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                StatusMessage = "E-posta adresiniz zaten onaylanmýþ durumda.";
                return RedirectToPage();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // ONAY SAYFASININ ADINI VE YOLUNU KENDÝ PROJENÝZE GÖRE AYARLAYIN
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailPage", // Bu isimde bir sayfa oluþturmanýz gerekecek
                pageHandler: null,
                values: new { userId = userId, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "E-posta Adresinizi Onaylayýn - DogukanStore",
                $"Lütfen DogukanStore hesabýnýzý <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>buraya týklayarak</a> onaylayýn.");

            StatusMessage = "Doðrulama e-postasý gönderildi. Lütfen e-posta kutunuzu kontrol edin.";
            _logger.LogInformation("Kullanýcý ({UserId}) için e-posta ({Email}) doðrulama maili tekrar gönderildi.", userId, email);
            return RedirectToPage();
        }
    }
}