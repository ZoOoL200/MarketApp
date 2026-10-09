using System.ComponentModel.DataAnnotations;
using MarketApp.Application.Common.Security;

namespace MarketApp.Application.DTOs.Users;

public class CreateUserRequestDto : IValidatableObject
{
    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(1024, MinimumLength = 4)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public List<Guid> BranchIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Role != AppRoles.BranchManager
            && Role != AppRoles.Seller)
        {
            yield return new ValidationResult(
                "Role must be BranchManager or Seller.",
                [nameof(Role)]);
        }

        if (BranchIds is null)
        {
            yield break;
        }

        if (BranchIds.Any(id => id == Guid.Empty))
        {
            yield return new ValidationResult(
                "Branch IDs cannot be empty.",
                [nameof(BranchIds)]);
        }

        if (BranchIds.Distinct().Count() != BranchIds.Count)
        {
            yield return new ValidationResult(
                "Branch IDs cannot contain duplicates.",
                [nameof(BranchIds)]);
        }
    }
}