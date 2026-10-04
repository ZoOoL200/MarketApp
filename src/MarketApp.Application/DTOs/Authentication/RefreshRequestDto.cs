using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Authentication;

public class RefreshRequestDto
{
    [Required]
    [StringLength(256)]
    public string RefreshToken { get; set; } = string.Empty;
}