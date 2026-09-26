using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Stock;

public class StockQueryDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 20;

    public Guid? ProductId { get; set; }

    public Guid? InventoryLocationId { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var offset = ((long)PageNumber - 1) * PageSize;

        if (offset > int.MaxValue)
        {
            yield return new ValidationResult(
                "The requested page number is too large.",
                [nameof(PageNumber)]);
        }

        if (ProductId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Provide a valid product ID or omit the filter.",
                [nameof(ProductId)]);
        }

        if (InventoryLocationId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Provide a valid inventory location ID " +
                "or omit the filter.",
                [nameof(InventoryLocationId)]);
        }
    }
}