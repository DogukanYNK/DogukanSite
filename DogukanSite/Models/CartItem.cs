// Models/CartItem.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogukanSite.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        // Giriş yapmış kullanıcılar için User ID
        public string? ApplicationUserId { get; set; } // Nullable string
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        // Misafir kullanıcılar veya kullanıcıya henüz atanmamış sepetler için Session ID
        // Bu alan, kullanıcı giriş yaptığında ApplicationUserId'ye dönüştürülebilir.
        [Required]
        public string SessionId { get; set; } = string.Empty; // Varsayılan değer ataması
    }
}