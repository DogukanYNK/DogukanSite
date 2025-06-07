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
    public class IndexModel : PageModel
    {
        private readonly DogukanSiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(DogukanSiteContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Ýstatistik Kartlarý için
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingOrders { get; set; }

        // Son Sipariþler Listesi için
        public List<Models.Order> RecentOrders { get; set; }

        public async Task OnGetAsync()
        {
            // Kart verilerini hesapla
            TotalRevenue = await _context.Orders
                .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                .SumAsync(o => o.TotalAmount);

            TotalOrders = await _context.Orders.CountAsync();
            TotalCustomers = await _userManager.Users.CountAsync();
            PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);

            // Son 5 sipariþi al
            RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();
        }

        // YENÝ EKLENEN METOT: Satýþ grafiði için JSON verisi döndürür
        public async Task<JsonResult> OnGetSalesDataAsync()
        {
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-29); // Son 30 gün

            // Ýptal veya iade edilmemiþ sipariþleri al
            var salesData = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .OrderBy(r => r.Date)
                .ToListAsync();

            // Son 30 günün her günü için bir etiket oluþtur
            var allDates = Enumerable.Range(0, 30).Select(i => startDate.AddDays(i));

            var labels = allDates.Select(d => d.ToString("dd MMM")).ToList();
            var data = allDates.Select(d =>
                salesData.FirstOrDefault(s => s.Date == d)?.Total ?? 0
            ).ToList();

            return new JsonResult(new { labels, data });
        }

        // YENÝ EKLENEN METOT: En çok satan ürünler grafiði için JSON verisi döndürür
        public async Task<JsonResult> OnGetTopProductsDataAsync()
        {
            var topProducts = await _context.OrderItems
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new
                {
                    ProductName = g.Key.Name,
                    TotalQuantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(7)
                .ToListAsync();

            var labels = topProducts.Select(p => p.ProductName).ToList();
            var data = topProducts.Select(p => p.TotalQuantity).ToList();

            return new JsonResult(new { labels, data });
        }

        // YENÝ EKLENEN METOT: Kategorilere göre satýþ daðýlýmý için JSON verisi döndürür
        public async Task<JsonResult> OnGetCategoryRevenueDataAsync()
        {
            var categorySales = await _context.OrderItems
                .Where(oi => oi.Product.Category != null && oi.Order.Status != OrderStatus.Cancelled && oi.Order.Status != OrderStatus.Refunded)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.PriceAtTimeOfPurchase)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(6) // En çok ciro getiren ilk 6 kategoriyi göster
                .ToListAsync();

            var labels = categorySales.Select(c => c.CategoryName).ToList();
            var data = categorySales.Select(c => c.TotalRevenue).ToList();

            return new JsonResult(new { labels, data });
        }

    }
}