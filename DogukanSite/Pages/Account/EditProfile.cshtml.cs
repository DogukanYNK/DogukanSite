// ===== DogukanSite/Pages/Account/EditProfile.cshtml.cs =====

using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization; // [Authorize] için eklendi
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize] // Bu sayfanýn güvenliði için Authorize attribute'ü aktif olmalý
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
            [Required(ErrorMessage = "Ad alaný zorunludur.")]
            [Display(Name = "Adýnýz")]
            [StringLength(50)]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Soyad alaný zorunludur.")]
            [Display(Name = "Soyadýnýz")]
            [StringLength(50)]
            public string LastName { get; set; }

            [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz.")]
            [Display(Name = "Telefon Numaranýz")]
            public string PhoneNumber { get; set; }

            [Display(Name = "E-posta Adresiniz")]
            public string Email { get; set; }
        }

        // Kullanýcý verisini getiren ve InputModel'i dolduran yardýmcý metot
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
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' ile bulunamadý.");
            }

            await LoadUserAsync(user);

            // --- GÜNCELLEME ---
            // Ana soldaki menü için "Profil" grubunu aktif yap
            ViewData["ActivePage"] = "Profile";
            // Profil sekmeleri için "Edit" sekmesini aktif yap
            ViewData["ActiveProfilePage"] = "Edit";

            ViewData["Title"] = "Profilimi Düzenle";
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' ile bulunamadý.");
            }

            if (!ModelState.IsValid)
            {
                await LoadUserAsync(user); // Hata durumunda formu tekrar doldur
                return Page();
            }

            // Deðiþiklikleri kontrol et ve güncelle
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                StatusMessage = "Hata: Profil güncellenirken beklenmedik bir hata oluþtu.";
                return RedirectToPage();
            }

            // Telefon numarasýný ayrýca kontrol et ve güncelle
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Hata: Telefon numarasý güncellenirken bir hata oluþtu.";
                    return RedirectToPage();
                }
            }

            // Kullanýcýnýn cookie bilgisini yenileyerek header gibi yerlerdeki bilgilerin güncellenmesini saðla
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Profiliniz baþarýyla güncellendi.";
            return RedirectToPage();
        }
    }
}