using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogukanSite.Models
{
    // Models/Product.cs
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ürün adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        public string? Description { get; set; } // Nullable olabilir

        [Required(ErrorMessage = "Fiyat alanı zorunludur.")]
        [Range(0.01, 1000000, ErrorMessage = "Fiyat 0.01 ile 1,000,000 arasında olmalıdır.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Url(ErrorMessage = "Lütfen geçerli bir URL giriniz.")]
        [StringLength(500)]
        public string? ImageUrl { get; set; } // Nullable olabilir

        [Required(ErrorMessage = "Kategori alanı zorunludur.")]
        [StringLength(50)]
        public string Category { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stok adedi negatif olamaz.")]
        public int Stock { get; set; } = 0; // Varsayılan stok 0 olabilir

        public bool IsFeatured { get; set; } = false; // Öne çıkan ürün mü?
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // İlişkiler (Eğer varsa)
        // public virtual ICollection<OrderItem>? OrderItems { get; set; }
        // public virtual ICollection<CartItem>? CartItems { get; set; }
        // public virtual ICollection<Favorite>? Favorites { get; set; }
    }
}
