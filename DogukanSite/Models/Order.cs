// DogukanSite/Models/Order.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // DatabaseGenerated attribute'u ve Column tipi için

namespace DogukanSite.Models
{
    public class Order
    {
        public int Id { get; set; }

        // CustomerName zaten vardı, DataAnnotations güncellenebilir
        [Required(ErrorMessage = "Müşteri adı ve soyadı zorunludur.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Ad Soyad alanı en az 3, en fazla 100 karakter olmalıdır.")]
        public string CustomerName { get; set; }

        // Mevcut Address alanı yerine veya ek olarak daha detaylı adres bilgileri
        [Required(ErrorMessage = "Teslimat adresi zorunludur.")]
        [StringLength(500)]
        public string ShippingAddress { get; set; } // Yeni: Tam teslimat adresi

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; } // Yeni: Telefon numarası (nullable olabilir)

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(100)]
        public string? Email { get; set; } // Yeni: E-posta (nullable olabilir)

        public DateTime OrderDate { get; set; }

        [Column(TypeName = "decimal(18,2)")] // Veritabanında para birimi için uygun tip
        public decimal OrderTotal { get; set; } // Yeni: Siparişin genel toplamı

        [StringLength(50)]
        public string OrderStatus { get; set; } // Yeni: Sipariş durumu (Pending, Shipped, Delivered, Cancelled)

        public string? OrderNotes { get; set; } // Yeni: Müşteri sipariş notları (nullable)

        // Kullanıcı ile ilişki (eğer kullanıcı giriş yapmışsa)
        public string? UserId { get; set; } // Yeni: Identity kullanıcısının ID'si (nullable)
        public virtual ApplicationUser? User { get; set; } // Yeni: Navigasyon özelliği (virtual ve nullable)

        // SessionId zaten vardı, misafir kullanıcılar için
        public string? SessionId { get; set; } // Nullable olabilir

        public List<OrderItem> Items { get; set; } = new List<OrderItem>(); // Başlangıçta boş liste ata
    }

    public class OrderItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } // Navigasyon özelliği için 'virtual'

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")] // Veritabanında para birimi için uygun tip
        public decimal UnitPrice { get; set; } // Yeni: Sipariş anındaki ürün fiyatı

        public int OrderId { get; set; }
        public virtual Order Order { get; set; } // Navigasyon özelliği için 'virtual'
    }
}