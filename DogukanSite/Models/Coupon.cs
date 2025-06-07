using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogukanSite.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kupon kodu zorunludur.")]
        [StringLength(50)]
        public string Code { get; set; } // Örn: YAZ2024

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        public string Description { get; set; } // Örn: Yaz Sezonu %20 İndirimi

        [Required]
        public DiscountType DiscountType { get; set; } // Yüzdesel mi, Sabit Tutar mı?

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // İndirim miktarı (15 veya 50 gibi)

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumSpend { get; set; } = 0; // Kuponun geçerli olması için min. harcama

        public DateTime? ExpiryDate { get; set; } // Kuponun son kullanma tarihi (opsiyonel)

        public bool IsActive { get; set; } = true; // Kupon aktif mi?
    }
}