using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Authentication;

public class ResetPasswordRequestDto
{
    public Guid UserId { get; set; }

    [Required]
    [StringLength(4096)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(
        1024,
        MinimumLength = 4,
        ErrorMessage = "Use a password between 4 and 1024 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(1024)]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}