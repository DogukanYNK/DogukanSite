using System.ComponentModel.DataAnnotations;

namespace DogukanSite.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }

        [Required]  // Geçici olarak kullanıcıyı tanımak için SessionId gerekli
        public string SessionId { get; set; }
    }
}
