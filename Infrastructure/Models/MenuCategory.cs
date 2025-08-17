using System.ComponentModel.DataAnnotations;
namespace Infrastructure.Models;
public class MenuCategory
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public List<MenuItem> Items { get; set; } = new();
}
