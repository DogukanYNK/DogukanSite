// ===== DogukanSite/Pages/Account/PersonalDataPage.cshtml.cs =====

using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class PersonalDataPageModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
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
            // --- YENÝ YAPI ÝÇÝN GÜNCELLEME ---
            ViewData["Title"] = "Kiþisel Veriler";
            // Ana soldaki menü için "Profil" grubunu aktif yap
            ViewData["ActivePage"] = "Profile";
            // Profil sekmeleri için "PersonalData" sekmesini aktif yap
            ViewData["ActiveProfilePage"] = "PersonalData";

            return Page();
        }

        public async Task<IActionResult> OnPostDownload() // "Async" soneki kaldýrýldý
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            _logger.LogInformation("User with ID '{UserId}' asked for their personal data.", _userManager.GetUserId(User));

            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(IdentityUser).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await _userManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            personalData.Add($"Authenticator Key", await _userManager.GetAuthenticatorKeyAsync(user));

            Response.Headers.Append("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json");
        }

        public async Task<IActionResult> OnPostDelete() // "Async" soneki kaldýrýldý
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!await _userManager.CheckPasswordAsync(user, DeleteInput.Password))
            {
                ModelState.AddModelError(string.Empty, "Girilen þifre yanlýþ.");
                return Page();
            }

            var result = await _userManager.DeleteAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unexpected error occurred deleting user with ID '{userId}'.");
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

            return Redirect("~/");
        }
    }
}