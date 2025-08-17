using System.ComponentModel.DataAnnotations;
namespace Infrastructure.Models;
public class Restaurant
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Slug { get; set; } = string.Empty; // used in QR URL
    public List<DiningTable> Tables { get; set; } = new();
    public List<MenuCategory> Categories { get; set; } = new();
}
