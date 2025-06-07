using DogukanSite.Models; // ApplicationUser i�in
using Microsoft.AspNetCore.Authentication; // Harici giri�ler i�in
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // IEmailSender i�in (opsiyonel)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities; // EncodeEmailForLink i�in
using System.Collections.Generic; // IList i�in
using System.ComponentModel.DataAnnotations;
using System.Linq;             // Harici giri�ler i�in
using System.Text;             // EncodeEmailForLink i�in
using System.Text.Encodings.Web;  // HtmlEncoder i�in (opsiyonel)
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ILogger i�in eklendi

namespace DogukanSite.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        // private readonly IEmailSender _emailSender; // E-posta onay� i�in (opsiyonel)

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger/*,
            IEmailSender emailSender*/)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            // _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } // Harici giri�ler i�in

        public class InputModel
        {
            [Required(ErrorMessage = "Ad alan� zorunludur.")]
            [StringLength(50, ErrorMessage = "{0} alan� en az {2} en fazla {1} karakter uzunlu�unda olmal�d�r.", MinimumLength = 2)]
            [Display(Name = "Ad")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Soyad alan� zorunludur.")]
            [StringLength(50, ErrorMessage = "{0} alan� en az {2} en fazla {1} karakter uzunlu�unda olmal�d�r.", MinimumLength = 2)]
            [Display(Name = "Soyad")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "E-posta alan� zorunludur.")]
            [EmailAddress(ErrorMessage = "L�tfen ge�erli bir e-posta adresi giriniz.")]
            [Display(Name = "E-posta")]
            public string Email { get; set; }

            [Required(ErrorMessage = "�ifre alan� zorunludur.")]
            [StringLength(100, ErrorMessage = "{0} alan� en az {2} en fazla {1} karakter uzunlu�unda olmal�d�r.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "�ifre")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "�ifreyi Do�rula")]
            [Compare("Password", ErrorMessage = "�ifre ile �ifre do�rulama alanlar� e�le�miyor.")]
            public string ConfirmPassword { get; set; }

            // [Range] attribute'� kald�r�ld�.
            [Display(Name = "Kullan�c� S�zle�mesi ve Gizlilik Politikas�n� okudum, kabul ediyorum.")]
            public bool AgreeToTerms { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/"); // Varsay�lan olarak ana sayfaya y�nlendir
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // ****** MANUEL KONTROL EKLEND� ******
            if (!Input.AgreeToTerms)
            {
                ModelState.AddModelError("Input.AgreeToTerms", "Devam etmek i�in kullan�c� s�zle�mesini kabul etmelisiniz.");
            }
            // ***************************************

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    RegistrationDate = DateTime.UtcNow // YEN� EKLENEN SATIR
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Yeni kullan�c� �ifre ile hesap olu�turdu.");

                    // Yeni kullan�c�ya varsay�lan "Customer" rol�n� ata
                    await _userManager.AddToRoleAsync(user, "Customer"); // "Customer" rol�n�n var oldu�undan emin olun

                    // E-posta onay� k�sm� yorumda b�rak�ld�, gerekirse a��labilir.

                    await _signInManager.SignInAsync(user, isPersistent: false); // Kay�t sonras� otomatik giri�
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Bir �eyler ters giderse, formu tekrar g�ster
            return Page();
        }
    }
}