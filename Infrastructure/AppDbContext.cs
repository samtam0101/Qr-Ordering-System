using Microsoft.EntityFrameworkCore;
using Infrastructure.Models;

namespace Infrastructure;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Restaurant>().HasIndex(r => r.Slug).IsUnique();
        b.Entity<DiningTable>().HasIndex(t => new { t.RestaurantId, t.Code }).IsUnique();
        b.Entity<MenuCategory>().HasIndex(c => new { c.RestaurantId, c.SortOrder });
        b.Entity<MenuItem>().Property(p => p.Price).HasColumnType("numeric(12,2)");
        b.Entity<OrderItem>().Property(p => p.UnitPrice).HasColumnType("numeric(12,2)");
        b.Entity<Payment>().Property(p => p.Amount).HasColumnType("numeric(12,2)");
    }
}
