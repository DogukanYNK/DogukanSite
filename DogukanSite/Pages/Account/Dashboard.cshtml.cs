using DogukanSite.Models; // ApplicationUser i�in
using Microsoft.AspNetCore.Authorization; // [Authorize] attribute'u i�in
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations; // Display attribute'u i�in
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize] // Bu sayfan�n sadece giri� yapm�� kullan�c�lar taraf�ndan eri�ilebilir olmas�n� sa�lar
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // ��k�� yapmak i�in

        public DashboardModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public UserInfoViewModel UserInfo { get; set; }

        public class UserInfoViewModel
        {
            public string UserId { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string FullName => $"{FirstName} {LastName}".Trim();
            public string PhoneNumber { get; set; }
            // Eklemek istedi�iniz di�er bilgiler
        }


        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Bu durum normalde [Authorize] attribute'u nedeniyle olu�maz
                // ama bir g�vence olarak eklenebilir.
                return NotFound($"Kullan�c� ID '{_userManager.GetUserId(User)}' ile bulunamad�.");
            }

            UserInfo = new UserInfoViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
                // Di�er ApplicationUser �zelliklerini buraya ekleyebilirsiniz
            };

            ViewData["Title"] = "Hesab�m";
            return Page();
        }

        // ��k�� yapma i�lemi i�in (Layout'ta zaten vard� ama burada da olabilir)
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _signInManager.SignOutAsync();
            // �ste�e ba�l�: Loglama
            // _logger.LogInformation("Kullan�c� ��k�� yapt�.");
            return RedirectToPage("/Index"); // Ana sayfaya y�nlendir
        }
    }
}