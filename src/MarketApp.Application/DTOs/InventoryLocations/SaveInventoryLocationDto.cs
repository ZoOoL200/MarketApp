using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.InventoryLocations;

public class SaveInventoryLocationDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}