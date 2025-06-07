using System.ComponentModel.DataAnnotations.Schema;

namespace DogukanSite.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Düzeltme: UserId artık nullable. Misafir sepetlerinde bu alan boş olacak.
        public string? UserId { get; set; }

        // Misafir sepetleri için session kimliği
        public string? SessionId { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}