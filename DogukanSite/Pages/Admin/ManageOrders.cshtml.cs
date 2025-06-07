using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageOrdersModel : PageModel
    {
        private readonly DogukanSiteContext _context;

        public ManageOrdersModel(DogukanSiteContext context)
        {
            _context = context;
        }

        public IList<OrderViewModel> OrdersVM { get; set; } = new List<OrderViewModel>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; }

        public List<SelectListItem> AllOrderStatuses { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 15; // Sayfa baþýna gösterilecek sipariþ sayýsý
        public int TotalPages { get; set; }
        public int TotalOrderCount { get; set; }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public string ContactName { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public string? UserEmail { get; set; } // Misafir sipariþlerinde boþ olabilir
        }

        public async Task OnGetAsync()
        {
            ViewData["Title"] = "Sipariþleri Yönet";

            var query = _context.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                if (int.TryParse(SearchTerm, out int orderId))
                {
                    query = query.Where(o => o.Id == orderId);
                }
                else
                {
                    query = query.Where(o =>
                        (o.User != null && (o.User.FirstName.Contains(SearchTerm) || o.User.LastName.Contains(SearchTerm) || o.User.Email.Contains(SearchTerm))) ||
                        o.ShippingContactName.Contains(SearchTerm) ||
                        o.GuestEmail.Contains(SearchTerm));
                }
            }

            if (!string.IsNullOrEmpty(StatusFilter))
            {
                if (Enum.TryParse<OrderStatus>(StatusFilter, out var statusEnum))
                {
                    query = query.Where(o => o.Status == statusEnum);
                }
            }

            if (!string.IsNullOrEmpty(DateFilter))
            {
                var today = DateTime.UtcNow.Date;
                switch (DateFilter)
                {
                    case "today":
                        query = query.Where(o => o.OrderDate.Date == today);
                        break;
                    case "last7days":
                        query = query.Where(o => o.OrderDate.Date >= today.AddDays(-7));
                        break;
                    case "last30days":
                        query = query.Where(o => o.OrderDate.Date >= today.AddDays(-30));
                        break;
                }
            }

            AllOrderStatuses = Enum.GetValues<OrderStatus>()
                                   .Select(status => new SelectListItem { Value = status.ToString(), Text = status.ToString() })
                                   .ToList();
            AllOrderStatuses.Insert(0, new SelectListItem { Value = "", Text = "Tüm Durumlar" });

            TotalOrderCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalOrderCount / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages == 0 ? 1 : TotalPages));

            var orders = await query
                                .OrderByDescending(o => o.OrderDate)
                                .Skip((CurrentPage - 1) * PageSize)
                                .Take(PageSize)
                                .ToListAsync();

            OrdersVM = orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                ContactName = o.ShippingContactName ?? (o.User != null ? $"{o.User.FirstName} {o.User.LastName}" : "Ýsimsiz"),
                UserEmail = o.User?.Email ?? o.GuestEmail,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
            }).ToList();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus)
        {
            var routeValues = new { currentPage = CurrentPage, searchTerm = SearchTerm, statusFilter = StatusFilter, dateFilter = DateFilter };

            if (orderId == 0 || string.IsNullOrEmpty(newStatus) || !Enum.TryParse<OrderStatus>(newStatus, out var statusEnum))
            {
                TempData["ErrorMessage"] = "Geçersiz sipariþ ID veya durum.";
                return RedirectToPage(routeValues);
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Güncellenecek sipariþ bulunamadý.";
                return RedirectToPage(routeValues);
            }

            order.Status = statusEnum;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Sipariþ #{orderId} durumu '{statusEnum}' olarak güncellendi.";
            return RedirectToPage(routeValues);
        }
    }
}