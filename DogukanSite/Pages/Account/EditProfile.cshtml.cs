// ===== DogukanSite/Pages/Account/EditProfile.cshtml.cs =====

using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization; // [Authorize] i�in eklendi
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize] // Bu sayfan�n g�venli�i i�in Authorize attribute'� aktif olmal�
    public class EditProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public EditProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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
            [Display(Name = "Telefon Numaran�z")]
            public string PhoneNumber { get; set; }

            [Display(Name = "E-posta Adresiniz")]
            public string Email { get; set; }
        }

        // Kullan�c� verisini getiren ve InputModel'i dolduran yard�mc� metot
        private async Task LoadUserAsync(ApplicationUser user)
        {
            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                Email = await _userManager.GetEmailAsync(user)
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' ile bulunamad�.");
            }

            await LoadUserAsync(user);

            // --- G�NCELLEME ---
            // Ana soldaki men� i�in "Profil" grubunu aktif yap
            ViewData["ActivePage"] = "Profile";
            // Profil sekmeleri i�in "Edit" sekmesini aktif yap
            ViewData["ActiveProfilePage"] = "Edit";

            ViewData["Title"] = "Profilimi D�zenle";
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' ile bulunamad�.");
            }

            if (!ModelState.IsValid)
            {
                await LoadUserAsync(user); // Hata durumunda formu tekrar doldur
                return Page();
            }

            // De�i�iklikleri kontrol et ve g�ncelle
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                StatusMessage = "Hata: Profil g�ncellenirken beklenmedik bir hata olu�tu.";
                return RedirectToPage();
            }

            // Telefon numaras�n� ayr�ca kontrol et ve g�ncelle
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Hata: Telefon numaras� g�ncellenirken bir hata olu�tu.";
                    return RedirectToPage();
                }
            }

            // Kullan�c�n�n cookie bilgisini yenileyerek header gibi yerlerdeki bilgilerin g�ncellenmesini sa�la
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Profiliniz ba�ar�yla g�ncellendi.";
            return RedirectToPage();
        }
    }
}