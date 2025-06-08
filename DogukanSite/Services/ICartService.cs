using DogukanSite.Pages.Cart;
using System.Threading.Tasks;

namespace DogukanSite.Services
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartViewModelAsync();
        Task<CartViewModel> UpdateItemQuantityAsync(int cartItemId, int quantity);
        Task<CartViewModel> RemoveItemAsync(int cartItemId);
        Task<CartViewModel> ApplyCouponAsync(string couponCode);
        Task<int> GetCartItemCountAsync();
        Task MergeCartsOnLoginAsync();
    }
}