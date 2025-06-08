using System;
using System.ComponentModel.DataAnnotations;

namespace DogukanSite.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        public DiscountType Type { get; set; } // YENİ: İndirim Tipi (Yüzdelik, Sabit Fiyat)

        [Required]
        public decimal Value { get; set; } // YENİ: İndirim Değeri

        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow; // YENİ: Başlangıç Tarihi

        [Required]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(30); // YENİ: Bitiş Tarihi

        public int? UsageLimit { get; set; } // YENİ: Kullanım Limiti (null ise limitsiz)
        public int UsageCount { get; set; } = 0; // YENİ: Kaç kez kullanıldığı

        public bool IsActive { get; set; } = true;
    }
}