using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Authentication;

public class ForgotPasswordRequestDto
{
    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;
}