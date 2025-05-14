using DogukanSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogukanSite.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var result = await _signInManager.PasswordSignInAsync(Email, Password, false, false);
            if (result.Succeeded)
            {
                return RedirectToPage("/Index");
            }

            ModelState.AddModelError(string.Empty, "Giriþ baþarýsýz.");
            return Page();
        }
    }

}
