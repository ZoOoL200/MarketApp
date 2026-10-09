using System.ComponentModel.DataAnnotations;
namespace MarketApp.Application.DTOs.Users;
public sealed class UpdateUserAccessDto
{
    public bool IsActive { get; set; } = true;
    [Required, MaxLength(100)] public List<Guid> BranchIds { get; set; } = [];
}
