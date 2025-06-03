// Pages/Account/PersonalDataPage.cshtml.cs
using DogukanSite.Models; // ApplicationUser için
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System; // Activator için
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json; // JsonSerializer için
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class PersonalDataPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // Hesap silme sonrasý çýkýþ için
        private readonly ILogger<PersonalDataPageModel> _logger;

        public PersonalDataPageModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<PersonalDataPageModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public DeleteAccountInputModel DeleteInput { get; set; }

        public class DeleteAccountInputModel
        {
            [Required(ErrorMessage = "Hesabýnýzý silmek için mevcut þifrenizi girmeniz zorunludur.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mevcut Þifreniz")]
            public string Password { get; set; }
        }


        public IActionResult OnGet()
        {
            // Bu sayfa yüklendiðinde özel bir veri yüklemeye gerek yok,
            // iþlemler POST ile tetiklenecek.
            // Eðer kullanýcý bilgisi göstermek isterseniz OnGetAsync yapýp user'ý çekebilirsiniz.
            DeleteInput = new DeleteAccountInputModel(); // Input modelini baþlat
            return Page();
        }

        public async Task<IActionResult> OnPostDownloadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            _logger.LogInformation("Kullanýcý ({UserId}) kiþisel verilerini indirme talebinde bulundu.", user.Id);

            // Identity tarafýndan yönetilen kiþisel verileri topla
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await _userManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} harici giriþ anahtarý", l.ProviderKey);
            }

            // Authenticator key'i (eðer varsa) ekle - dikkatli olun, bu hassas bir bilgidir.
            // Genellikle doðrudan export edilmez veya farklý bir yöntemle sunulur.
            // Þimdilik sadece 2FA'nýn aktif olup olmadýðýný ekleyelim.
            personalData.Add($"Ýki Aþamalý Doðrulama Etkin", (await _userManager.GetTwoFactorEnabledAsync(user)).ToString());

            // ÖNEMLÝ: Uygulamanýza özel diðer kiþisel verileri de buraya eklemelisiniz
            // (Örn: Sipariþ geçmiþi, adresler vb. - bu kýsým projenizin yapýsýna göre detaylandýrýlmalý)
            // Örnek:
            // var orders = await _context.Orders.Where(o => o.UserId == user.Id).ToListAsync();
            // personalData.Add("Sipariþ Sayýsý", orders.Count.ToString());

            Response.Headers.Append("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json; charset=utf-8");
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' bulunamadý.");
            }

            if (!ModelState.IsValid) // DeleteInput.Password için
            {
                return Page();
            }

            // Þifreyi doðrula
            var checkPasswordResult = await _userManager.CheckPasswordAsync(user, DeleteInput.Password);
            if (!checkPasswordResult)
            {
                ModelState.AddModelError("DeleteInput.Password", "Girilen þifre yanlýþ.");
                _logger.LogWarning("Kullanýcý ({UserId}) hesap silme denemesinde yanlýþ þifre girdi.", user.Id);
                return Page();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = $"Hata: Hesap silinirken beklenmedik bir sorun oluþtu.";
                _logger.LogError("Kullanýcý ({UserId}) silinemedi: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                // ModelState'e de eklenebilir
                foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); }
                return Page();
            }

            await _signInManager.SignOutAsync(); // Kullanýcýnýn oturumunu sonlandýr
            _logger.LogInformation("Kullanýcý ({UserId}) hesabýný kalýcý olarak sildi.", user.Id);
            StatusMessage = "Hesabýnýz baþarýyla silindi."; // Bu mesaj muhtemelen görünmeyecek çünkü ana sayfaya yönlendiriyoruz.
                                                            // Ana sayfada TempData'dan okunabilir.
            return RedirectToPage("/Index"); // Veya özel bir "Hesap Silindi" sayfasýna
        }
    }
}