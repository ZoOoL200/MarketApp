using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Users;

public sealed class CreateStakeholderRequestDto
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
}