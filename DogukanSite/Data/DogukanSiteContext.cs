// Data/DogukanSiteContext.cs
using DogukanSite.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DogukanSite.Data
{
    public class DogukanSiteContext : IdentityDbContext<ApplicationUser>
    {
        public DogukanSiteContext(DbContextOptions<DogukanSiteContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; } = default!; // null uyarıları için
        public DbSet<CartItem> CartItems { get; set; } = default!; // null uyarıları için
        public DbSet<Order> Orders { get; set; } = default!; // null uyarıları için
        public DbSet<OrderItem> OrderItems { get; set; } = default!; // null uyarıları için
        public DbSet<Favorite> Favorites { get; set; } = default!; // YENİ DbSet

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Favorite için bileşik unique index (bir kullanıcı aynı ürünü tekrar favorileyemez)
            builder.Entity<Favorite>()
                .HasIndex(f => new { f.ApplicationUserId, f.ProductId }).IsUnique();

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