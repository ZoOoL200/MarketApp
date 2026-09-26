using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Branches;

public class SaveBranchDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
}