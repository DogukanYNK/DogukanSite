using DogukanSite.Data;
using DogukanSite.Models;
using DogukanSite.Pages.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DogukanSite.Services
{
    public class OrderService : IOrderService
    {
        private readonly DogukanSiteContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly ICartService _cartService; // Sepet bilgilerini almak için

        public OrderService(DogukanSiteContext context, ILogger<OrderService> logger, ICartService cartService)
        {
            _context = context;
            _logger = logger;
            _cartService = cartService;
        }

        public async Task<CreateOrderResult> CreateOrderFromCartAsync(CreateModel.OrderInputModel orderInput, string userId, string sessionId)
        {
            var cart = await _cartService.GetCartViewModelAsync();
            if (cart.IsCartEmpty)
            {
                return new CreateOrderResult { Success = false, ErrorMessage = "Sepetiniz boş." };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Models.Order
                {
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending, // Sipariş durumu başlangıçta "Beklemede"
                    TotalAmount = cart.Total,
                    OrderNotes = orderInput.OrderNotes,
                    ShippingContactName = orderInput.ContactName,
                    ShippingPhoneNumber = orderInput.PhoneNumber,
                    ShippingStreet = orderInput.Street,
                    ShippingCity = orderInput.City,
                    ShippingState = orderInput.State,
                    ShippingPostalCode = orderInput.PostalCode,
                    ShippingCountry = "Türkiye",
                    UserId = userId,
                    GuestEmail = userId == null ? orderInput.Email : null
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Order ID'sinin oluşması için kaydet

                foreach (var cartItem in cart.Items)
                {
                    // --- GÜVENLİK VE VERİ BÜTÜNLÜĞÜ KONTROLÜ ---
                    var productInDb = await _context.Products.FindAsync(cartItem.ProductId);

                    if (productInDb == null || productInDb.Stock < cartItem.Quantity)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning("Stok yetersizliği veya ürün bulunamadı. Sipariş iptal edildi. ProductId: {ProductId}", cartItem.ProductId);
                        return new CreateOrderResult { Success = false, ErrorMessage = $"'{cartItem.ProductName}' adlı üründe yeterli stok kalmadı." };
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        // Sipariş anındaki güncel fiyatı kaydet
                        PriceAtTimeOfPurchase = productInDb.DiscountPrice ?? productInDb.Price
                    };
                    _context.OrderItems.Add(orderItem);

                    // Stok düşürme işlemi
                    productInDb.Stock -= cartItem.Quantity;
                }

                var cartItemsDb = await _context.CartItems.Where(ci => ci.ApplicationUserId == userId || ci.SessionId == sessionId).ToListAsync();
                _context.CartItems.RemoveRange(cartItemsDb);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Sipariş (ID: {OrderId}) başarıyla oluşturuldu.", order.Id);
                return new CreateOrderResult { Success = true, CreatedOrderId = order.Id };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Sipariş oluşturulurken bir hata oluştu.");
                return new CreateOrderResult { Success = false, ErrorMessage = "Siparişiniz oluşturulurken beklenmedik bir hata oluştu." };
            }
        }
    }
}