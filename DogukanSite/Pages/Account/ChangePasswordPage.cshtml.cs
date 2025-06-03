// Pages/Account/ChangePasswordPage.cshtml.cs
using DogukanSite.Models; // ApplicationUser için
using Microsoft.AspNetCore.Authorization; // [Authorize] için
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize] // Bu sayfa sadece giriþ yapmýþ kullanýcýlar tarafýndan eriþilebilir olmalý
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
            [Required(ErrorMessage = "Mevcut þifre alaný zorunludur.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mevcut Þifreniz")]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "Yeni þifre alaný zorunludur.")]
            [StringLength(100, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunluðunda olmalýdýr.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Yeni Þifreniz")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Yeni Þifreyi Doðrulayýn")]
            [Compare("NewPassword", ErrorMessage = "Yeni þifre ve doðrulama þifresi eþleþmiyor.")]
            public string ConfirmPassword { get; set; }
        }

        public bool HasPassword { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            HasPassword = await _userManager.HasPasswordAsync(user);
            if (!HasPassword)
            {
                // Eðer kullanýcý harici bir saðlayýcý ile giriþ yaptýysa ve yerel þifresi yoksa,
                // þifre belirleme sayfasýna yönlendirilebilir veya bu sayfa devre dýþý býrakýlabilir.
                // Þimdilik, bu durumda bir mesaj gösterip sayfada kalmasýný saðlayalým.
                StatusMessage = "Hata: Bu hesap için ayarlanmýþ bir yerel þifre bulunmuyor. Þifre belirlemek için farklý bir yöntem kullanmanýz gerekebilir.";
            }

            Input = new InputModel(); // Formu boþ baþlat
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            HasPassword = await _userManager.HasPasswordAsync(user);
            if (!HasPassword)
            {
                StatusMessage = "Hata: Bu hesap için bir þifre ayarlanamaz.";
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
                _logger.LogWarning("Kullanýcý ({UserId}) þifresini deðiþtirirken hata oluþtu: {Errors}", user.Id, string.Join(", ", changePasswordResult.Errors.Select(e => e.Description)));
                StatusMessage = "Hata: Þifreniz deðiþtirilirken bir sorun oluþtu. Lütfen girdiðiniz bilgileri kontrol edin.";
                return Page();
            }

            // Þifre deðiþtirildikten sonra kullanýcýnýn oturumunu yenilemek önemlidir.
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Kullanýcý ({UserId}) þifresini baþarýyla deðiþtirdi.", user.Id);
            StatusMessage = "Þifreniz baþarýyla deðiþtirildi.";

            return RedirectToPage(); // Ayný sayfaya yönlendirerek TempData mesajýnýn gösterilmesini saðla
        }
    }
}