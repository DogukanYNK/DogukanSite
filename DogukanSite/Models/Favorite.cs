// Models/Favorite.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogukanSite.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } // Kimin favorisi
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; } // Nullable olabilir

        public int ProductId { get; set; } // Hangi ürün
        [ForeignKey("ProductId")]
        public Product? Product { get; set; } // Nullable olabilir

        public DateTime AddedDate { get; set; } = DateTime.UtcNow; // Ne zaman eklendi
    }
}