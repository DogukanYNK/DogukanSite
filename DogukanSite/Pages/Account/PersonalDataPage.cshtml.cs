// Pages/Account/PersonalDataPage.cshtml.cs
using DogukanSite.Models; // ApplicationUser i�in
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System; // Activator i�in
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json; // JsonSerializer i�in
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class PersonalDataPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // Hesap silme sonras� ��k�� i�in
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
            [Required(ErrorMessage = "Hesab�n�z� silmek i�in mevcut �ifrenizi girmeniz zorunludur.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mevcut �ifreniz")]
            public string Password { get; set; }
        }


        public IActionResult OnGet()
        {
            // Bu sayfa y�klendi�inde �zel bir veri y�klemeye gerek yok,
            // i�lemler POST ile tetiklenecek.
            // E�er kullan�c� bilgisi g�stermek isterseniz OnGetAsync yap�p user'� �ekebilirsiniz.
            DeleteInput = new DeleteAccountInputModel(); // Input modelini ba�lat
            return Page();
        }

        public async Task<IActionResult> OnPostDownloadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            _logger.LogInformation("Kullan�c� ({UserId}) ki�isel verilerini indirme talebinde bulundu.", user.Id);

            // Identity taraf�ndan y�netilen ki�isel verileri topla
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
                personalData.Add($"{l.LoginProvider} harici giri� anahtar�", l.ProviderKey);
            }

            // Authenticator key'i (e�er varsa) ekle - dikkatli olun, bu hassas bir bilgidir.
            // Genellikle do�rudan export edilmez veya farkl� bir y�ntemle sunulur.
            // �imdilik sadece 2FA'n�n aktif olup olmad���n� ekleyelim.
            personalData.Add($"�ki A�amal� Do�rulama Etkin", (await _userManager.GetTwoFactorEnabledAsync(user)).ToString());

            // �NEML�: Uygulaman�za �zel di�er ki�isel verileri de buraya eklemelisiniz
            // (�rn: Sipari� ge�mi�i, adresler vb. - bu k�s�m projenizin yap�s�na g�re detayland�r�lmal�)
            // �rnek:
            // var orders = await _context.Orders.Where(o => o.UserId == user.Id).ToListAsync();
            // personalData.Add("Sipari� Say�s�", orders.Count.ToString());

            Response.Headers.Append("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json; charset=utf-8");
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' bulunamad�.");
            }

            if (!ModelState.IsValid) // DeleteInput.Password i�in
            {
                return Page();
            }

            // �ifreyi do�rula
            var checkPasswordResult = await _userManager.CheckPasswordAsync(user, DeleteInput.Password);
            if (!checkPasswordResult)
            {
                ModelState.AddModelError("DeleteInput.Password", "Girilen �ifre yanl��.");
                _logger.LogWarning("Kullan�c� ({UserId}) hesap silme denemesinde yanl�� �ifre girdi.", user.Id);
                return Page();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = $"Hata: Hesap silinirken beklenmedik bir sorun olu�tu.";
                _logger.LogError("Kullan�c� ({UserId}) silinemedi: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                // ModelState'e de eklenebilir
                foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); }
                return Page();
            }

            await _signInManager.SignOutAsync(); // Kullan�c�n�n oturumunu sonland�r
            _logger.LogInformation("Kullan�c� ({UserId}) hesab�n� kal�c� olarak sildi.", user.Id);
            StatusMessage = "Hesab�n�z ba�ar�yla silindi."; // Bu mesaj muhtemelen g�r�nmeyecek ��nk� ana sayfaya y�nlendiriyoruz.
                                                            // Ana sayfada TempData'dan okunabilir.
            return RedirectToPage("/Index"); // Veya �zel bir "Hesap Silindi" sayfas�na
        }
    }
}