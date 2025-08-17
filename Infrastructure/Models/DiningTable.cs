using System.ComponentModel.DataAnnotations;
namespace Infrastructure.Models;
public class DiningTable
{
    public int Id { get; set; }
    [Required, MaxLength(32)] public string Code { get; set; } = string.Empty; // shown on QR
    public int Seats { get; set; } = 2;
    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
}
