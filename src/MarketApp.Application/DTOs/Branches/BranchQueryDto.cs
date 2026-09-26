using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Branches;

public class BranchQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 20;

    public bool? IsActive { get; set; }
}