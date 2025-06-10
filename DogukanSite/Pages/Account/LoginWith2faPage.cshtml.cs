using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [AllowAnonymous]
    public class LoginWith2faPageModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginWith2faPageModel> _logger;

        public LoginWith2faPageModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginWith2faPageModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public bool RememberMe { get; set; }

        // ----- EKSÝK OLAN VE ÞÝMDÝ EKLENEN SATIR BURASI -----
        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Doðrulama kodu zorunludur.")]
            [StringLength(7, MinimumLength = 6, ErrorMessage = "{0} 6 ile 7 karakter arasýnda olmalýdýr.")]
            [DataType(DataType.Text)]
            [Display(Name = "Doðrulama Kodu")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Bu cihazý hatýrla")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Ýki aþamalý doðrulama için kullanýcý yüklenemedi.");
            }

            ReturnUrl = returnUrl;
            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            returnUrl ??= Url.Content("~/");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Ýki aþamalý doðrulama için kullanýcý yüklenemedi.");
            }

            var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("Kullanýcý ID '{UserId}' ile 2FA kullanarak giriþ yaptý.", user.Id);
                return LocalRedirect(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("Kullanýcý ID '{UserId}' hesabý kilitlendi.", user.Id);
                return RedirectToPage("./Lockout");
            }
            else
            {
                _logger.LogWarning("Kullanýcý ID '{UserId}' için geçersiz doðrulama kodu girildi.", user.Id);
                ModelState.AddModelError(string.Empty, "Geçersiz doðrulama kodu.");
                return Page();
            }
        }
    }
}