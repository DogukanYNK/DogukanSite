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

        // ----- EKS�K OLAN VE ��MD� EKLENEN SATIR BURASI -----
        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Do�rulama kodu zorunludur.")]
            [StringLength(7, MinimumLength = 6, ErrorMessage = "{0} 6 ile 7 karakter aras�nda olmal�d�r.")]
            [DataType(DataType.Text)]
            [Display(Name = "Do�rulama Kodu")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Bu cihaz� hat�rla")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"�ki a�amal� do�rulama i�in kullan�c� y�klenemedi.");
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
                throw new InvalidOperationException($"�ki a�amal� do�rulama i�in kullan�c� y�klenemedi.");
            }

            var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("Kullan�c� ID '{UserId}' ile 2FA kullanarak giri� yapt�.", user.Id);
                return LocalRedirect(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("Kullan�c� ID '{UserId}' hesab� kilitlendi.", user.Id);
                return RedirectToPage("./Lockout");
            }
            else
            {
                _logger.LogWarning("Kullan�c� ID '{UserId}' i�in ge�ersiz do�rulama kodu girildi.", user.Id);
                ModelState.AddModelError(string.Empty, "Ge�ersiz do�rulama kodu.");
                return Page();
            }
        }
    }
}