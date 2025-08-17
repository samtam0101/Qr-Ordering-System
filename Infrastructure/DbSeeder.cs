using Microsoft.EntityFrameworkCore;
using Infrastructure.Models;

namespace Infrastructure;
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Restaurants.AnyAsync()) return;

        var r1 = new Restaurant { Name = "Demo Resto", Slug = "demo" };
        r1.Tables.AddRange(new[] { new DiningTable { Code = "T1", Seats = 2 }, new DiningTable { Code = "T2", Seats = 4 } });
        var c1 = new MenuCategory { Name = "Starters", SortOrder = 1, Restaurant = r1 };
        var c2 = new MenuCategory { Name = "Mains", SortOrder = 2, Restaurant = r1 };
        c1.Items.AddRange(new[] { new MenuItem { Name = "Bruschetta", Price = 18, ImageUrl = null }, new MenuItem { Name = "Soup", Price = 15 } });
        c2.Items.AddRange(new[] { new MenuItem { Name = "Grilled Chicken", Price = 45 }, new MenuItem { Name = "Pasta Alfredo", Price = 39 } });

        var r2 = new Restaurant { Name = "Hotel Bistro", Slug = "bistro" };
        r2.Tables.AddRange(new[] { new DiningTable { Code = "H1", Seats = 2 }, new DiningTable { Code = "H2", Seats = 6 } });
        var c3 = new MenuCategory { Name = "Breakfast", SortOrder = 1, Restaurant = r2 };
        c3.Items.AddRange(new[] { new MenuItem { Name = "Omelette", Price = 20 }, new MenuItem { Name = "Pancakes", Price = 22 } });

        db.AddRange(r1, c1, c2, r2, c3);
        await db.SaveChangesAsync();
    }
}
