using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // ILogger eklemek iyi bir pratiktir
using System.Threading.Tasks;     // Task için eklendi

namespace DogukanSite.Pages.Account
{
    // [Authorize] attribute'üne burada gerek yok, çünkü çýkýþ iþlemi yapýyoruz.
    // Ancak kalmasýnda bir sakýnca da yok.
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        // OnGet metoduna artýk ihtiyacýmýz yok veya boþ olabilir.
        public IActionResult OnGet()
        {
            // Genellikle GET ile çýkýþ istenmez, doðrudan ana sayfaya yönlendirebiliriz.
            return RedirectToPage("/Index");
        }

        // **** DEÐÝÞÝKLÝK BURADA: OnGet -> OnPostAsync ****
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // Çýkýþ iþlemini yap
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Kullanýcý çýkýþ yaptý.");

            // Eðer formdan bir returnUrl geldiyse oraya, gelmediyse ana sayfaya yönlendir.
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // returnUrl formda @Url.Page("/Index") olarak ayarlandýðý için
                // genellikle buraya düþmez ama bir güvence olarak kalabilir.
                return RedirectToPage("/Index");
            }
        }
    }
}