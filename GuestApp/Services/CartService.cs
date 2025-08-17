using Infrastructure;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace GuestApp.Services
{
    public class CartService
    {
        private readonly AppDbContext _db;

        private string? _slug;
        private string? _tableCode;
        private int? _restaurantId;
        private int? _tableId;

        private Order? _draft;

        public CartService(AppDbContext db) => _db = db;

        public async Task InitAsync(string? restaurantSlug, string? tableCode)
        {
            if (string.IsNullOrWhiteSpace(restaurantSlug) || string.IsNullOrWhiteSpace(tableCode))
                throw new InvalidOperationException("Restaurant slug and table code are required.");

            _slug = restaurantSlug;
            _tableCode = tableCode;

            // Auto-provision demo data
            if (string.Equals(_slug, "demo", StringComparison.OrdinalIgnoreCase))
                await EnsureDemoDataAsync();

            // Resolve ids (avoid null navs)
            var rest = await _db.Restaurants.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Slug == _slug)
                ?? throw new InvalidOperationException("Restaurant not found.");

            var table = await _db.DiningTables.AsNoTracking()
                .FirstOrDefaultAsync(t => t.RestaurantId == rest.Id && t.Code == _tableCode)
                ?? throw new InvalidOperationException("Table not found.");

            _restaurantId = rest.Id;
            _tableId = table.Id;

            _draft = await FindDraftAsync();
        }

        private async Task<Order?> FindDraftAsync()
        {
            if (_restaurantId is null || _tableId is null) return null;

            return await _db.Orders
                .Include(o => o.Items)!.ThenInclude(i => i.MenuItem)
                .Where(o => o.Status == OrderStatus.Draft
                            && o.RestaurantId == _restaurantId
                            && o.DiningTableId == _tableId)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<Order> EnsureDraftCreatedAsync()
        {
            _draft ??= await FindDraftAsync();
            if (_draft != null) return _draft;

            if (_restaurantId is null || _tableId is null)
                throw new InvalidOperationException("Cart context is not initialized.");

            _draft = new Order
            {
                RestaurantId = _restaurantId.Value,
                DiningTableId = _tableId.Value,
                Status = OrderStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };

            _db.Orders.Add(_draft);
            await _db.SaveChangesAsync();
            return _draft;
        }

        public async Task<int> AddItemAsync(int menuItemId, int qty = 1)
        {
            if (qty <= 0) qty = 1;

            var order = await EnsureDraftCreatedAsync();
            var items = order.Items ??= new List<OrderItem>();

            var existing = items.FirstOrDefault(i => i.MenuItemId == menuItemId);
            if (existing == null)
            {
                var menu = await _db.MenuItems.FirstAsync(m => m.Id == menuItemId);
                existing = new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = menuItemId,
                    Quantity = qty,
                    UnitPrice = menu.Price
                };
                items.Add(existing);
            }
            else
            {
                existing.Quantity += qty;
            }

            await _db.SaveChangesAsync();
            return order.Id;
        }

        public async Task<int> RemoveItemAsync(int menuItemId, int qty = 1)
        {
            _draft ??= await FindDraftAsync();
            if (_draft == null) return 0;

            var items = _draft.Items;
            if (items == null || items.Count == 0) return 0;

            var it = items.FirstOrDefault(i => i.MenuItemId == menuItemId);
            if (it == null) return items.Sum(i => i.Quantity);

            it.Quantity -= qty;
            if (it.Quantity <= 0) items.Remove(it);

            await _db.SaveChangesAsync();
            return items.Sum(i => i.Quantity);
        }

        public async Task<Order?> GetDraftAsync()
        {
            _draft ??= await FindDraftAsync();
            if (_draft == null) return null;

            _draft = await _db.Orders
                .Include(o => o.Items)!.ThenInclude(i => i.MenuItem)
                .FirstAsync(o => o.Id == _draft!.Id);

            return _draft;
        }

        public decimal ComputeTotal(Order? order)
            => order?.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;

        public async Task<int> PlaceOrderAsync()
        {
            var order = await GetDraftAsync();
            if (order == null || order.Items == null || order.Items.Count == 0)
                throw new InvalidOperationException("Your cart is empty.");

            order.Status = OrderStatus.Submitted; // no SubmittedAt field needed
            await _db.SaveChangesAsync();

            var id = order.Id;
            _draft = null; // clear cached draft
            return id;
        }

        public async Task<List<Order>> GetMyOrdersAsync()
        {
            if (_restaurantId is null || _tableId is null) return new();

            return await _db.Orders
                .Include(o => o.Items)!.ThenInclude(i => i.MenuItem)
                .Where(o => o.DiningTableId == _tableId
                            && o.RestaurantId == _restaurantId
                            && o.Status != OrderStatus.Cancelled)
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        // ---- demo seeding ----
        private async Task EnsureDemoDataAsync()
{
    // Look for existing "demo" restaurant (ONLY include Tables; avoid Category references)
    var existing = await _db.Restaurants
        .Include(r => r.Tables)
        .FirstOrDefaultAsync(r => r.Slug == "demo");

    if (existing != null)
    {
        // Ensure the demo table "D1" exists
        if (existing.Tables == null || !existing.Tables.Any(t => t.Code == "D1"))
        {
            existing.Tables ??= new List<DiningTable>();
            existing.Tables.Add(new DiningTable { Code = "D1", Seats = 2 });
            await _db.SaveChangesAsync();
        }

        // Ensure there are at least a couple of menu items in the system.
        // (No Category references; attach to Restaurant if that property exists.)
        if (!await _db.MenuItems.AnyAsync())
        {
            var m1 = new MenuItem { Name = "Cheeseburger", Price = 8.50m, Description = "Beef patty, cheese, bun" };
            var m2 = new MenuItem { Name = "Cola", Price = 1.50m, Description = "Chilled soft drink" };

            // If your MenuItem has a RestaurantId property, set it via reflection (avoids compile dependency).
            var ridProp = typeof(MenuItem).GetProperty("RestaurantId");
            if (ridProp != null)
            {
                ridProp.SetValue(m1, existing.Id);
                ridProp.SetValue(m2, existing.Id);
            }

            _db.MenuItems.AddRange(m1, m2);
            await _db.SaveChangesAsync();
        }

        return;
    }

    // Create brand-new demo restaurant with one table (no Category usage)
    var demo = new Restaurant
    {
        Name = "Demo Restaurant",
        Slug = "demo",
        Tables = new List<DiningTable>
        {
            new DiningTable { Code = "D1", Seats = 2 }
        }
    };

    _db.Restaurants.Add(demo);
    await _db.SaveChangesAsync();

    // Add a couple of menu items (no Category)
    if (!await _db.MenuItems.AnyAsync())
    {
        var b = new MenuItem { Name = "Cheeseburger", Price = 8.50m, Description = "Beef patty, cheese, bun" };
        var d = new MenuItem { Name = "Cola", Price = 1.50m, Description = "Chilled soft drink" };

        // Attach to restaurant if MenuItem has RestaurantId
        var ridProp = typeof(MenuItem).GetProperty("RestaurantId");
        if (ridProp != null)
        {
            ridProp.SetValue(b, demo.Id);
            ridProp.SetValue(d, demo.Id);
        }

        _db.MenuItems.AddRange(b, d);
        await _db.SaveChangesAsync();
    }
}

    }
}
