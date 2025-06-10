using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class EmailManagementPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Services.IEmailSender _emailSender; // Kendi IEmailSender'ýnýz
        private readonly ILogger<EmailManagementPageModel> _logger;

        public EmailManagementPageModel(
            UserManager<ApplicationUser> userManager,
            Services.IEmailSender emailSender,
            ILogger<EmailManagementPageModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
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
            Input = new InputModel { NewEmail = CurrentEmail };
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

            // --- YENÝ YAPI ÝÇÝN GÜNCELLEME ---
            ViewData["Title"] = "E-posta Yönetimi";
            ViewData["ActivePage"] = "Security";
            ViewData["ActiveSecurityPage"] = "EmailManagement";

            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmail() // Handler adý düzeltildi
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            if (!ModelState.IsValid)
            {
                await LoadUserDataAsync(user);
                return Page();
            }

            var currentEmail = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail.Equals(currentEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Girdiðiniz e-posta adresi mevcut adresinizle ayný.";
                return RedirectToPage();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailChangePage",
                pageHandler: null,
                values: new { area = "Identity", userId, email = Input.NewEmail, code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.NewEmail,
                "E-posta Adresinizi Onaylayýn",
                $"Lütfen e-posta adresinizi <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>buraya týklayarak</a> onaylayýn.");

            StatusMessage = "Onay linki yeni e-posta adresinize gönderildi.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmail() // Handler adý düzeltildi
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailPage",
                pageHandler: null,
                values: new { area = "Identity", userId, code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "E-posta Adresinizi Onaylayýn",
                $"Lütfen hesabýnýzý <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>buraya týklayarak</a> onaylayýn.");

            StatusMessage = "Doðrulama e-postasý gönderildi. Lütfen e-postanýzý kontrol edin.";
            return RedirectToPage();
        }
    }
}