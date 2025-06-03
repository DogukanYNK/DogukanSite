using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // ILogger eklemek iyi bir pratiktir
using System.Threading.Tasks;     // Task i�in eklendi

namespace DogukanSite.Pages.Account
{
    // [Authorize] attribute'�ne burada gerek yok, ��nk� ��k�� i�lemi yap�yoruz.
    // Ancak kalmas�nda bir sak�nca da yok.
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        // OnGet metoduna art�k ihtiyac�m�z yok veya bo� olabilir.
        public IActionResult OnGet()
        {
            // Genellikle GET ile ��k�� istenmez, do�rudan ana sayfaya y�nlendirebiliriz.
            return RedirectToPage("/Index");
        }

        // **** DE����KL�K BURADA: OnGet -> OnPostAsync ****
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // ��k�� i�lemini yap
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Kullan�c� ��k�� yapt�.");

            // E�er formdan bir returnUrl geldiyse oraya, gelmediyse ana sayfaya y�nlendir.
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // returnUrl formda @Url.Page("/Index") olarak ayarland��� i�in
                // genellikle buraya d��mez ama bir g�vence olarak kalabilir.
                return RedirectToPage("/Index");
            }
        }
    }
}