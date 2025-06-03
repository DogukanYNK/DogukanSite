using DogukanSite.Models; // ApplicationUser için
using Microsoft.AspNetCore.Authorization; // [Authorize] attribute'u için
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations; // Display attribute'u için
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize] // Bu sayfanýn sadece giriþ yapmýþ kullanýcýlar tarafýndan eriþilebilir olmasýný saðlar
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // Çýkýþ yapmak için

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
            // Eklemek istediðiniz diðer bilgiler
        }


        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Bu durum normalde [Authorize] attribute'u nedeniyle oluþmaz
                // ama bir güvence olarak eklenebilir.
                return NotFound($"Kullanýcý ID '{_userManager.GetUserId(User)}' ile bulunamadý.");
            }

            UserInfo = new UserInfoViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
                // Diðer ApplicationUser özelliklerini buraya ekleyebilirsiniz
            };

            ViewData["Title"] = "Hesabým";
            return Page();
        }

        // Çýkýþ yapma iþlemi için (Layout'ta zaten vardý ama burada da olabilir)
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _signInManager.SignOutAsync();
            // Ýsteðe baðlý: Loglama
            // _logger.LogInformation("Kullanýcý çýkýþ yaptý.");
            return RedirectToPage("/Index"); // Ana sayfaya yönlendir
        }
    }
}