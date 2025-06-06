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


        // --- YENİ EKLENECEK ALANLAR ---

        // Bu ürün "Sizin İçin Seçtiklerimiz" gibi özel bir bölümde mi gösterilsin?
        public bool IsFeatured { get; set; } = false;

        // Bu ürün "Yeni Gelenler" bölümünde mi gösterilsin?
        public bool IsNewArrival { get; set; } = false;

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}
