using DogukanSite.Models; // ApplicationUser i�in
using Microsoft.AspNetCore.Authentication; // IAuthenticationSchemeProvider i�in (Harici giri�ler i�in)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic; // IList i�in
using System.ComponentModel.DataAnnotations; // DataAnnotations i�in
using System.Linq;                // Harici giri�ler i�in
using System.Threading.Tasks;     // Asenkron i�in

namespace DogukanSite.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger; // Loglama i�in eklendi

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } // Harici giri�ler i�in

        public string ReturnUrl { get; set; } // Kullan�c�y� giri� sonras� y�nlendirmek i�in

        [TempData] // Hata mesaj�n� TempData ile ta��yal�m
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "E-posta alan� zorunludur.")]
            [EmailAddress(ErrorMessage = "L�tfen ge�erli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta")]
            public string Email { get; set; }

            [Required(ErrorMessage = "�ifre alan� zorunludur.")]
            [DataType(DataType.Password)]
            [Display(Name = "�ifre")]
            public string Password { get; set; }

            [Display(Name = "Beni Hat�rla?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/"); // Varsay�lan olarak ana sayfaya y�nlendir

            // Mevcut harici �erezi temizle (varsa)
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/"); // Varsay�lan olarak ana sayfaya y�nlendir
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid) // Model do�rulamas� yap�ld�
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Kullan�c� giri� yapt�.");
                    return LocalRedirect(returnUrl); // G�venlik i�in LocalRedirect kullan
                }
                if (result.RequiresTwoFactor)
                {
                    // �ki fakt�rl� kimlik do�rulama i�in y�nlendirme (e�er ayarl�ysa)
                    return RedirectToPage("./LoginWith2faPage", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Kullan�c� hesab� kilitlendi.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Giri� denemesi ba�ar�s�z. L�tfen e-posta ve �ifrenizi kontrol edin.");
                    return Page();
                }
            }

            // Bir �eyler ters giderse, formu tekrar g�ster
            return Page();
        }

        // Harici giri�ler i�in POST handler (opsiyonel)
        public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            // Harici giri� sa�lay�c�s�na y�nlendirme iste�i
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }
    }
}