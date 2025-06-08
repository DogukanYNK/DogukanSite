using System.ComponentModel.DataAnnotations;

namespace DogukanSite.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        // --- YENİ: Kullanıcı ve Misafir (Session) Bağlantıları ---

        // Kayıtlı kullanıcılar için
        public string? ApplicationUserId { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }

        // Misafir kullanıcılar için
        public string? SessionId { get; set; }
    }
}