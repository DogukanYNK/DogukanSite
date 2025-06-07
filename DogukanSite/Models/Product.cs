using System.ComponentModel.DataAnnotations.Schema;

namespace DogukanSite.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; } // İndirimli Fiyat

        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }

        // HATA GİDERİLDİ: Eksik olan DateAdded alanı eklendi.
        // Yeni bir ürün oluşturulduğunda otomatik olarak o anın tarihini atar.
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public string? ImageUrl { get; set; }
        public int Stock { get; set; }

        // Kargo için özellikler
        public double? Weight { get; set; } // kg
        public double? Width { get; set; } // cm
        public double? Height { get; set; } // cm
        public double? Length { get; set; } // cm

        // YENİ EKLENEN ALAN
        public bool IsNewArrival { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsBestSeller { get; set; }

        // Foreign Key
        public int CategoryId { get; set; }

        // Navigation Properties
        public virtual Category Category { get; set; }
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public virtual ICollection<Favorite> FavoritedByUsers { get; set; } = new List<Favorite>();
    }
}