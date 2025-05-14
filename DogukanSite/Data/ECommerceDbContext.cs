using DogukanSite.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DogukanSite.Data
{
    public class ECommerceDbContext : IdentityDbContext<ApplicationUser>
    {
        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
    }

}
