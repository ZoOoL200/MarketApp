using System.ComponentModel.DataAnnotations;

namespace MarketApp.Api.Models;

public class UploadProductPhotoRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
}

public class UpdateProductPhotoOrderRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int? SortOrder { get; set; }
}
