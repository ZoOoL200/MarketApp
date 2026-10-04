using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Authentication;

public class LoginRequestDto
{
    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(1024)]
    public string Password { get; set; } = string.Empty;
}