namespace DogukanSite.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }

        // YENİ EKLENEN ALAN
        public DateTime AddedDate { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; }
        public virtual Product Product { get; set; }
    }
}   