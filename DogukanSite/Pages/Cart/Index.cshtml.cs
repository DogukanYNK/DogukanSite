using DogukanSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DogukanSite.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ICartService cartService, ILogger<IndexModel> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        public CartViewModel Cart { get; set; } = new CartViewModel();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("Sepet sayfasý (OnGetAsync) yükleniyor.");
            Cart = await _cartService.GetCartViewModelAsync();
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostUpdateQuantityJsonAsync(int cartItemId, int quantity)
        {
            var cartViewModel = await _cartService.UpdateItemQuantityAsync(cartItemId, quantity);
            return new JsonResult(cartViewModel);
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostRemoveItemJsonAsync(int cartItemId)
        {
            var cartViewModel = await _cartService.RemoveItemAsync(cartItemId);
            return new JsonResult(cartViewModel);
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostApplyCouponJsonAsync(string couponCode)
        {
            var cartViewModel = await _cartService.ApplyCouponAsync(couponCode);
            return new JsonResult(cartViewModel);
        }
    }

    // Bu yardýmcý sýnýflar burada veya ayrý bir dosyada olabilir.
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public string AppliedCouponCode { get; set; }
        public decimal Total { get; set; }
        public bool IsCartEmpty => !Items.Any();
        public string CouponResponseMessage { get; set; }
        public bool CouponAppliedSuccessfully { get; set; }
    }

    public class CartItemViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "Ürün Adý Yok";
        public string? ProductImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public int MaxQuantity { get; set; }
    }
}