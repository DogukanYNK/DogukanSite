namespace DogukanSite.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; }

        public int Rating { get; set; } // 1-5 arası puan
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}