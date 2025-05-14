using System.ComponentModel.DataAnnotations;

namespace DogukanSite.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]  // Örneğin müşteri adı için doğrulama ekledim
        public string CustomerName { get; set; }

        [Required]
        public string Address { get; set; }

        public DateTime OrderDate { get; set; }

        public List<OrderItem> Items { get; set; }

        public string SessionId { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
    }
}