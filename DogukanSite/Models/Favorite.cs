using System;
using System.ComponentModel.DataAnnotations;

namespace DogukanSite.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        // Tutarlılık için ApplicationUserId olarak güncellendi
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    }
}