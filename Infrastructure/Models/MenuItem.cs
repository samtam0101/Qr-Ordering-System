using System.ComponentModel.DataAnnotations;
namespace Infrastructure.Models;
public class MenuItem
{
    public int Id { get; set; }
    public int MenuCategoryId { get; set; }
    public MenuCategory? MenuCategory { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(400)] public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? ImageUrl { get; set; }
}
