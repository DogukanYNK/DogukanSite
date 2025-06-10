using DogukanSite.Models;
using DogukanSite.Pages.Cart;
using DogukanSite.Pages.Order;
using System.Threading.Tasks;

namespace DogukanSite.Services
{
    // Sipariş oluşturma sonucunu taşıyacak yardımcı sınıf
    public class CreateOrderResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int? CreatedOrderId { get; set; }
    }

    public interface IOrderService
    {
        Task<CreateOrderResult> CreateOrderFromCartAsync(CreateModel.OrderInputModel orderInput, string userId, string sessionId);
    }
}