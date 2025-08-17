using System.ComponentModel.DataAnnotations;
namespace Infrastructure.Models;
public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public int DiningTableId { get; set; }
    public DiningTable? DiningTable { get; set; }
    [MaxLength(64)] public string? GuestSessionId { get; set; } // cookie session
    public List<OrderItem> Items { get; set; } = new();
    public Payment? Payment { get; set; }
    public decimal Total => Items.Sum(i => i.Quantity * i.UnitPrice);
}
