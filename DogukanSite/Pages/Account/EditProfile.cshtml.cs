// Pages/Account/EditProfile.cshtml.cs
using DogukanSite.Data; // DbContext'iniz i�in
using DogukanSite.Models; // ApplicationUser i�in
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    // Bu sayfan�n sadece giri� yapm�� kullan�c�lar taraf�ndan eri�ilebilir olmas� gerekir.
    // E�er t�m /Account klas�r� i�in bir Authorize filtresi uygulamad�ysan�z, buraya [Authorize] ekleyebilirsiniz.
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
            [Required(ErrorMessage = "Ad alan� zorunludur.")]
            [Display(Name = "Ad�n�z")]
            [StringLength(50)]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Soyad alan� zorunludur.")]
            [Display(Name = "Soyad�n�z")]
            [StringLength(50)]
            public string LastName { get; set; }

            [Phone(ErrorMessage = "Ge�erli bir telefon numaras� giriniz.")]
            [Display(Name = "Telefon Numaras�")]
            public string PhoneNumber { get; set; }

            // E-posta genellikle ayr� bir "E-posta Y�netimi" sayfas�nda ve onay s�reciyle de�i�tirilir.
            // Bu formda sadece g�stermek i�in veya salt okunur olabilir.
            [Display(Name = "E-posta Adresi")]
            public string Email { get; set; } // Salt okunur olacak
        }

        private async Task LoadCurrentUserAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user); // E-posta genellikle kullan�c� ad�d�r
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Input = new InputModel
            {
                FirstName = user.FirstName, // ApplicationUser modelinizde FirstName oldu�undan emin olun
                LastName = user.LastName,   // ApplicationUser modelinizde LastName oldu�undan emin olun
                PhoneNumber = phoneNumber,
                Email = userName // Veya user.Email, hangisi do�ruysa
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Bu durum normalde [Authorize] attribute'u ile engellenir.
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            await LoadCurrentUserAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCurrentUserAsync(user); // Hatal� durumda formu tekrar doldur
                return Page();
            }

            // FirstName ve LastName'i ApplicationUser modelinize ekledi�inizi varsay�yorum.
            // E�er yoksa, ApplicationUser s�n�f�n�z� geni�letmeniz gerekir.
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
                    StatusMessage = "Hata: Telefon numaras� g�ncellenirken beklenmedik bir hata olu�tu.";
                    _logger.LogError("Telefon numaras� g�ncellenemedi: {Errors}", string.Join(", ", setPhoneResult.Errors.Select(e => e.Description)));
                    return RedirectToPage();
                }
                changed = true;
            }

            if (changed)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Hata: Profil g�ncellenirken beklenmedik bir hata olu�tu.";
                    _logger.LogError("Profil g�ncellenemedi: {Errors}", string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    return RedirectToPage();
                }
            }

            // Kullan�c�n�n cookie'sini yenilemek �nemlidir, b�ylece g�ncel bilgiler (�rn: Ad� Soyad�) header'da vb. yans�r.
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Profiliniz ba�ar�yla g�ncellendi.";
            _logger.LogInformation("Kullan�c� ({UserId}) profilini g�ncelledi.", user.Id);
            return RedirectToPage();
        }
    }
}