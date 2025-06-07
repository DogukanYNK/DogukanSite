using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogukanSite.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Düzeltme: UserId artık nullable. Misafir siparişlerinde bu alan boş olacak.
        public string? UserId { get; set; }

        // Düzeltme: Yeni alan. Misafir siparişinin e-postasını saklamak için.
        [EmailAddress]
        public string? GuestEmail { get; set; }

        public DateTime OrderDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public string? ShippingProvider { get; set; }
        public string? TrackingNumber { get; set; }
        public string ShippingContactName { get; set; }
        public string ShippingStreet { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingPostalCode { get; set; }
        public string ShippingCountry { get; set; }
        public string? OrderNotes { get; set; }

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}