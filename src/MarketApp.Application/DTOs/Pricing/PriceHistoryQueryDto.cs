using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Pricing;

public class PriceHistoryQueryDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var offset = ((long)PageNumber - 1) * PageSize;

        if (offset > int.MaxValue)
        {
            yield return new ValidationResult(
                "The requested page is too large.",
                [nameof(PageNumber)]);
        }
    }
}