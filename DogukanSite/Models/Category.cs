using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DogukanSite.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kategori adı 100 karakterden uzun olamaz.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Açıklama 500 karakterden uzun olamaz.")]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        [Display(Name = "Öne Çıkan")]
        public bool IsFeatured { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Kategori Görsel URL")]
        public string? ImageUrl { get; set; }

        // DEĞİŞTİRİLDİ: ParentCategory'nin null olabileceğini belirtmek için '?' eklendi.
        public virtual Category? ParentCategory { get; set; }

        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}