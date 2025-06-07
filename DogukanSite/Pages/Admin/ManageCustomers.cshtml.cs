using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageCustomersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DogukanSiteContext _context;

        public ManageCustomersModel(UserManager<ApplicationUser> userManager, DogukanSiteContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<CustomerViewModel> Customers { get; set; } = new List<CustomerViewModel>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalPages { get; set; }
        public int TotalCustomerCount { get; set; }

        public class CustomerViewModel
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public DateTime RegistrationDate { get; set; }
            public int OrderCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(u => u.FirstName.Contains(SearchTerm) || u.LastName.Contains(SearchTerm) || u.Email.Contains(SearchTerm));
            }

            TotalCustomerCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCustomerCount / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages == 0 ? 1 : TotalPages));

            var users = await query
                .OrderByDescending(u => u.RegistrationDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Her kullanýcý için sipariþ sayýsýný verimli bir þekilde alalým
            var userIds = users.Select(u => u.Id).ToList();
            var orderCounts = await _context.Orders
                .Where(o => userIds.Contains(o.UserId))
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            Customers = users.Select(u => new CustomerViewModel
            {
                Id = u.Id,
                FullName = $"{u.FirstName} {u.LastName}",
                Email = u.Email,
                RegistrationDate = u.RegistrationDate,
                OrderCount = orderCounts.ContainsKey(u.Id) ? orderCounts[u.Id] : 0
            }).ToList();
        }
    }
}