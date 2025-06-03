// Pages/Account/EditProfile.cshtml.cs
using DogukanSite.Data; // DbContext'iniz için
using DogukanSite.Models; // ApplicationUser için
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    // Bu sayfanýn sadece giriþ yapmýþ kullanýcýlar tarafýndan eriþilebilir olmasý gerekir.
    // Eðer tüm /Account klasörü için bir Authorize filtresi uygulamadýysanýz, buraya [Authorize] ekleyebilirsiniz.
    // [Authorize] 
    public class EditProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<EditProfileModel> _logger;

        public EditProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<EditProfileModel> logger)
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
            [Required(ErrorMessage = "Ad alaný zorunludur.")]
            [Display(Name = "Adýnýz")]
            [StringLength(50)]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Soyad alaný zorunludur.")]
            [Display(Name = "Soyadýnýz")]
            [StringLength(50)]
            public string LastName { get; set; }

            [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz.")]
            [Display(Name = "Telefon Numarasý")]
            public string PhoneNumber { get; set; }

            // E-posta genellikle ayrý bir "E-posta Yönetimi" sayfasýnda ve onay süreciyle deðiþtirilir.
            // Bu formda sadece göstermek için veya salt okunur olabilir.
            [Display(Name = "E-posta Adresi")]
            public string Email { get; set; } // Salt okunur olacak
        }

        private async Task LoadCurrentUserAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user); // E-posta genellikle kullanýcý adýdýr
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Input = new InputModel
            {
                FirstName = user.FirstName, // ApplicationUser modelinizde FirstName olduðundan emin olun
                LastName = user.LastName,   // ApplicationUser modelinizde LastName olduðundan emin olun
                PhoneNumber = phoneNumber,
                Email = userName // Veya user.Email, hangisi doðruysa
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Bu durum normalde [Authorize] attribute'u ile engellenir.
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            await LoadCurrentUserAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCurrentUserAsync(user); // Hatalý durumda formu tekrar doldur
                return Page();
            }

            // FirstName ve LastName'i ApplicationUser modelinize eklediðinizi varsayýyorum.
            // Eðer yoksa, ApplicationUser sýnýfýnýzý geniþletmeniz gerekir.
            bool changed = false;
            if (user.FirstName != Input.FirstName)
            {
                user.FirstName = Input.FirstName;
                changed = true;
            }
            if (user.LastName != Input.LastName)
            {
                user.LastName = Input.LastName;
                changed = true;
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Hata: Telefon numarasý güncellenirken beklenmedik bir hata oluþtu.";
                    _logger.LogError("Telefon numarasý güncellenemedi: {Errors}", string.Join(", ", setPhoneResult.Errors.Select(e => e.Description)));
                    return RedirectToPage();
                }
                changed = true;
            }

            if (changed)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Hata: Profil güncellenirken beklenmedik bir hata oluþtu.";
                    _logger.LogError("Profil güncellenemedi: {Errors}", string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    return RedirectToPage();
                }
            }

            // Kullanýcýnýn cookie'sini yenilemek önemlidir, böylece güncel bilgiler (örn: Adý Soyadý) header'da vb. yansýr.
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Profiliniz baþarýyla güncellendi.";
            _logger.LogInformation("Kullanýcý ({UserId}) profilini güncelledi.", user.Id);
            return RedirectToPage();
        }
    }
}