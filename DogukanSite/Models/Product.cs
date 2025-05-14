using System.ComponentModel.DataAnnotations;

namespace DogukanSite.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        // Kategori enum ile daha yönetilebilir hale getirilebilir.
        public string Category { get; set; }
    }
}
