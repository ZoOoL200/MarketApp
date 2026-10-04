using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Authentication;

public class ConfirmEmailRequestDto
{
    public Guid UserId { get; set; }

    [Required]
    [StringLength(4096)]
    public string Token { get; set; } = string.Empty;
}