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
                .HasIndex(f => new { f.UserId, f.ProductId }).IsUnique();

            // YENİ EKLENEN KURAL
            // Coupon 'Code' alanının benzersiz (unique) olmasını sağlar
            builder.Entity<Coupon>()
                .HasIndex(c => c.Code).IsUnique();


            // CartItem için (isteğe bağlı ama mantıklı olabilir):
            // Bir kullanıcı için aynı ürün sepette birden fazla satırda olmamalı, sadece adedi artmalı.
            // VEYA bir session için aynı ürün birden fazla satırda olmamalı.
            // Bu kuralı ApplicationUserId null ise SessionId + ProductId, değilse ApplicationUserId + ProductId olarak kurmak karmaşık olabilir.
            // Şimdilik bu kuralı eklemiyorum, uygulama katmanında bu kontrolü yapmak daha esnek olabilir.
            // Ama istersen eklenebilir:
            // builder.Entity<CartItem>()
            //    .HasIndex(ci => new { ci.ApplicationUserId, ci.ProductId }).IsUnique(); // Bu, ApplicationUserId null ise sorun yaratır.
            // Daha karmaşık bir unique index veya ayrı kontrol gerekir.

            // Ondalık sayılar için hassasiyet (eğer Product modelinde Price decimal ise)
            // builder.Entity<Product>()
            //    .Property(p => p.Price)
            //    .HasColumnType("decimal(18,2)");

        }
    }
}