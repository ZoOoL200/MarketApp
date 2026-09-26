using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Products;

public class SaveProductDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Sku { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}