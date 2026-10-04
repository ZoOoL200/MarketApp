using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Pricing;

public class UpdateBranchPriceDto : IValidatableObject
{
    [Range(typeof(decimal), "0", "1000000000")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int ExpectedRevision { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (decimal.Round(Price, 4) != Price)
        {
            yield return new ValidationResult(
                "Price supports at most 4 decimal places.",
                [nameof(Price)]);
        }
    }
}