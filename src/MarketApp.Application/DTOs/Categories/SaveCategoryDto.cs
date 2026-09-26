using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Categories;

public class SaveCategoryDto
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, ErrorMessage =
        "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;
}