// Pages/Account/ChangePasswordPage.cshtml.cs
using DogukanSite.Models; // ApplicationUser i�in
using Microsoft.AspNetCore.Authorization; // [Authorize] i�in
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize] // Bu sayfa sadece giri� yapm�� kullan�c�lar taraf�ndan eri�ilebilir olmal�
    public class ChangePasswordPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ChangePasswordPageModel> _logger;

        public ChangePasswordPageModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ChangePasswordPageModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Mevcut �ifre alan� zorunludur.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mevcut �ifreniz")]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "Yeni �ifre alan� zorunludur.")]
            [StringLength(100, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunlu�unda olmal�d�r.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Yeni �ifreniz")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Yeni �ifreyi Do�rulay�n")]
            [Compare("NewPassword", ErrorMessage = "Yeni �ifre ve do�rulama �ifresi e�le�miyor.")]
            public string ConfirmPassword { get; set; }
        }

        public bool HasPassword { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            HasPassword = await _userManager.HasPasswordAsync(user);
            if (!HasPassword)
            {
                // E�er kullan�c� harici bir sa�lay�c� ile giri� yapt�ysa ve yerel �ifresi yoksa,
                // �ifre belirleme sayfas�na y�nlendirilebilir veya bu sayfa devre d��� b�rak�labilir.
                // �imdilik, bu durumda bir mesaj g�sterip sayfada kalmas�n� sa�layal�m.
                StatusMessage = "Hata: Bu hesap i�in ayarlanm�� bir yerel �ifre bulunmuyor. �ifre belirlemek i�in farkl� bir y�ntem kullanman�z gerekebilir.";
            }

            Input = new InputModel(); // Formu bo� ba�lat
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            HasPassword = await _userManager.HasPasswordAsync(user);
            if (!HasPassword)
            {
                StatusMessage = "Hata: Bu hesap i�in bir �ifre ayarlanamaz.";
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                _logger.LogWarning("Kullan�c� ({UserId}) �ifresini de�i�tirirken hata olu�tu: {Errors}", user.Id, string.Join(", ", changePasswordResult.Errors.Select(e => e.Description)));
                StatusMessage = "Hata: �ifreniz de�i�tirilirken bir sorun olu�tu. L�tfen girdi�iniz bilgileri kontrol edin.";
                return Page();
            }

            // �ifre de�i�tirildikten sonra kullan�c�n�n oturumunu yenilemek �nemlidir.
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Kullan�c� ({UserId}) �ifresini ba�ar�yla de�i�tirdi.", user.Id);
            StatusMessage = "�ifreniz ba�ar�yla de�i�tirildi.";

            return RedirectToPage(); // Ayn� sayfaya y�nlendirerek TempData mesaj�n�n g�sterilmesini sa�la
        }
    }
}