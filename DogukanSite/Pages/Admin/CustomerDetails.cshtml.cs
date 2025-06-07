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

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CustomerDetailsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerDetailsModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public CustomerViewModel Customer { get; set; }

        public class CustomerViewModel
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public DateTime RegistrationDate { get; set; }
            public List<Address> Addresses { get; set; }
            public List<Models.Order> Orders { get; set; }

            // Hesaplanan Ýstatistikler
            public int TotalOrderCount => Orders?.Count ?? 0;
            public decimal TotalSpent => Orders?.Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded).Sum(o => o.TotalAmount) ?? 0;
        }


        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("Müþteri ID'si belirtilmemiþ.");
            }

            var user = await _userManager.Users
                .Include(u => u.Addresses)
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound($"ID'si {id} olan müþteri bulunamadý.");
            }

            Customer = new CustomerViewModel
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RegistrationDate = user.RegistrationDate,
                Addresses = user.Addresses.ToList(),
                Orders = user.Orders.OrderByDescending(o => o.OrderDate).ToList()
            };

            ViewData["Title"] = $"Müþteri Detayý: {Customer.FullName}";
            return Page();
        }
    }
}