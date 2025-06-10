using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Account
{
    [Authorize]
    public class AddressBookModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddressBookModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Address> Addresses { get; set; }

        [BindProperty]
        public Address Address { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            Addresses = await _context.Addresses
                                      .Where(a => a.UserId == userId)
                                      .OrderByDescending(a => a.IsDefaultShipping)
                                      .ThenByDescending(a => a.IsDefaultBilling)
                                      .ThenBy(a => a.AddressTitle)
                                      .ToListAsync();

            ViewData["Title"] = "Adres Defterim";
            ViewData["ActivePage"] = "Profile";
            ViewData["ActiveProfilePage"] = "AddressBook";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);
            Address.UserId = userId;

            if (!ModelState.IsValid)
            {
                Addresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
                return Page();
            }

            // Gerekirse diðer varsayýlanlarý güncelle
            await UpdateDefaultAddresses(userId, Address);

            if (Address.Id == 0)
            {
                _context.Addresses.Add(Address);
                StatusMessage = "Yeni adres baþarýyla eklendi.";
            }
            else
            {
                var addressFromDb = await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == Address.Id && a.UserId == userId);
                if (addressFromDb == null) return Forbid();

                _context.Addresses.Update(Address);
                StatusMessage = "Adres baþarýyla güncellendi.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null) return Forbid();

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
            StatusMessage = "Adres baþarýyla silindi.";

            return RedirectToPage();
        }

        // Yardýmcý metot
        private async Task UpdateDefaultAddresses(string userId, Address currentAddress)
        {
            if (currentAddress.IsDefaultShipping)
            {
                var otherShippingAddresses = await _context.Addresses.Where(a => a.UserId == userId && a.Id != currentAddress.Id).ToListAsync();
                otherShippingAddresses.ForEach(a => a.IsDefaultShipping = false);
            }
            if (currentAddress.IsDefaultBilling)
            {
                var otherBillingAddresses = await _context.Addresses.Where(a => a.UserId == userId && a.Id != currentAddress.Id).ToListAsync();
                otherBillingAddresses.ForEach(a => a.IsDefaultBilling = false);
            }
        }
    }
}