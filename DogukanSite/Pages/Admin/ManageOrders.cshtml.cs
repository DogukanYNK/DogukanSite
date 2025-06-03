using DogukanSite.Data;
using DogukanSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // SelectListItem için

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
        public string SearchTerm { get; set; } // Sipariþ ID veya Müþteri Adý için

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }
        public List<SelectListItem> AllOrderStatuses { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; } // "today", "last7days", "last30days"

        // Sayfalama için
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalOrderCount { get; set; }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } // Veya Kullanýcý Adý/E-postasý
            public DateTime OrderDate { get; set; }
            public decimal OrderTotal { get; set; }
            public string OrderStatus { get; set; }
            public string UserId { get; set; }
            public string UserEmail { get; set; } // Kullanýcý e-postasýný da gösterelim
        }

        public async Task OnGetAsync()
        {
            ViewData["Title"] = "Sipariþleri Yönet";

            var query = _context.Orders.Include(o => o.User).AsQueryable(); // User bilgisini çekmek için Include

            // Arama (Sipariþ ID veya Müþteri Adý/E-postasý)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                // Sipariþ ID'si sayýsal mý diye kontrol et
                if (int.TryParse(SearchTerm, out int orderId))
                {
                    query = query.Where(o => o.Id == orderId ||
                                             (o.User != null && (o.User.FirstName.Contains(SearchTerm) || o.User.LastName.Contains(SearchTerm) || o.User.Email.Contains(SearchTerm))) ||
                                             o.CustomerName.Contains(SearchTerm));
                }
                else
                {
                    query = query.Where(o => (o.User != null && (o.User.FirstName.Contains(SearchTerm) || o.User.LastName.Contains(SearchTerm) || o.User.Email.Contains(SearchTerm))) ||
                                             o.CustomerName.Contains(SearchTerm));
                }
            }

            // Durum Filtreleme
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                query = query.Where(o => o.OrderStatus == StatusFilter);
            }

            // Tarih Filtreleme
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

            // Sipariþ durumlarýný al (filtreleme dropdown'ý için)
            // Order modelinde OrderStatus alanýnýn string olduðunu varsayýyoruz.
            // Gerçekte bir enum veya ayrý bir tablo olabilir.
            AllOrderStatuses = await _context.Orders
                                        .Select(o => o.OrderStatus)
                                        .Where(s => !string.IsNullOrEmpty(s))
                                        .Distinct()
                                        .OrderBy(s => s)
                                        .Select(s => new SelectListItem { Value = s, Text = s })
                                        .ToListAsync();
            AllOrderStatuses.Insert(0, new SelectListItem { Value = "", Text = "Tüm Durumlar" });


            TotalOrderCount = await query.CountAsync();
            TotalPages = (int)System.Math.Ceiling(TotalOrderCount / (double)PageSize);
            CurrentPage = System.Math.Max(1, System.Math.Min(CurrentPage, TotalPages == 0 ? 1 : TotalPages));

            var orders = await query
                                .OrderByDescending(o => o.OrderDate) // En yeni sipariþler üste
                                .Skip((CurrentPage - 1) * PageSize)
                                .Take(PageSize)
                                .ToListAsync();

            OrdersVM = orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                CustomerName = o.User?.FirstName + " " + o.User?.LastName ?? o.CustomerName, // Kullanýcý varsa adýný, yoksa sipariþteki adý
                UserEmail = o.User?.Email ?? o.Email, // Kullanýcý varsa e-postasýný, yoksa sipariþteki e-postayý
                OrderDate = o.OrderDate,
                OrderTotal = o.OrderTotal,
                OrderStatus = o.OrderStatus ?? "Bilinmiyor",
                UserId = o.UserId
            }).ToList();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus)
        {
            if (orderId == 0 || string.IsNullOrEmpty(newStatus))
            {
                TempData["ErrorMessage"] = "Geçersiz sipariþ ID veya durum.";
                return RedirectToPage(new { currentPage = CurrentPage, searchTerm = SearchTerm, statusFilter = StatusFilter, dateFilter = DateFilter });
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Güncellenecek sipariþ bulunamadý.";
                return RedirectToPage(new { currentPage = CurrentPage, searchTerm = SearchTerm, statusFilter = StatusFilter, dateFilter = DateFilter });
            }

            order.OrderStatus = newStatus;
            // Ýsteðe baðlý: Sipariþ durumu deðiþtiðinde bir log tutulabilir veya kullanýcýya e-posta gönderilebilir.
            // Örneðin: if (newStatus == "Shipped") { /* Kargo takip no gir, e-posta gönder */ }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Sipariþ #{orderId} durumu '{newStatus}' olarak güncellendi.";
            return RedirectToPage(new { currentPage = CurrentPage, searchTerm = SearchTerm, statusFilter = StatusFilter, dateFilter = DateFilter });
        }
    }
}