using Infrastructure;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

public class OrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) { _db = db; }

    public async Task AddToCartAsync(Order draft, int menuItemId, int qty, string? notes)
    {
        var item = await _db.MenuItems.FirstAsync(m => m.Id == menuItemId);
        var line = draft.Items.FirstOrDefault(i => i.MenuItemId == menuItemId && i.Notes == notes);
        if (line == null)
        {
            line = new OrderItem { MenuItemId = menuItemId, Quantity = qty, UnitPrice = item.Price, Notes = notes };
            draft.Items.Add(line);
        }
        else line.Quantity += qty;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateQtyAsync(int orderItemId, int qty)
    {
        var line = await _db.OrderItems.FindAsync(orderItemId);
        if (line == null) return;
        line.Quantity = Math.Max(1, qty);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveLineAsync(int orderItemId)
    {
        var line = await _db.OrderItems.FindAsync(orderItemId);
        if (line == null) return;
        _db.OrderItems.Remove(line);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> PlaceOrderAsync(int orderId)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null || order.Items.Count == 0) return false;
        order.Status = OrderStatus.New;
        order.Payment = new Payment { Amount = order.Total, Status = PaymentStatus.Pending, Provider = "DEMO" };
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DemoPayAsync(int orderId)
    {
        var order = await _db.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;
        if (order.Payment == null) order.Payment = new Payment { OrderId = order.Id, Amount = order.Total, Status = PaymentStatus.Pending, Provider = "DEMO" };
        order.Payment.Status = PaymentStatus.Paid;
        await _db.SaveChangesAsync();
        return true;
    }
}
