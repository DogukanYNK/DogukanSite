using DogukanSite.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DogukanSite.Data
{
    public class DogukanSiteContext : IdentityDbContext<ApplicationUser>
    {
        public DogukanSiteContext(DbContextOptions<DogukanSiteContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        // YENİ EKLENEN DbSet'ler
        public DbSet<Category> Categories { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Address> Addresses { get; set; }

        // YENİ EKLENEN DbSet
        public DbSet<Coupon> Coupons { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Favorite için bileşik unique index (bir kullanıcı aynı ürünü tekrar favorileyemez)
            builder.Entity<Favorite>()
                .HasIndex(f => new { f.ApplicationUserId, f.ProductId }).IsUnique();

            // YENİ EKLENEN KURAL
            // Coupon 'Code' alanının benzersiz (unique) olmasını sağlar
            builder.Entity<Coupon>()
                .HasIndex(c => c.Code).IsUnique();

        }
    }
}