using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.StockTransfers;

public class CreateStockTransferDto : IValidatableObject
{
    public Guid SourceLocationId { get; set; }

    public Guid DestinationLocationId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public List<CreateStockTransferLineDto> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (SourceLocationId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Source location is required.",
                [nameof(SourceLocationId)]);
        }

        if (DestinationLocationId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Destination location is required.",
                [nameof(DestinationLocationId)]);
        }

        if (SourceLocationId == DestinationLocationId)
        {
            yield return new ValidationResult(
                "Source and destination must be different.",
                [nameof(DestinationLocationId)]);
        }

        if (Lines is not null && Lines.Any(l => l is null))
        {
            yield return new ValidationResult(
                "Transfer lines cannot contain null entries.",
                [nameof(Lines)]);
        }
    }
}

public class CreateStockTransferLineDto : IValidatableObject
{
    public Guid ProductId { get; set; }

    [Range(typeof(decimal), "0.001", "1000000000")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal BaselineUnitPrice { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (ProductId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Product is required.",
                [nameof(ProductId)]);
        }

        if (decimal.Round(Quantity, 3) != Quantity)
        {
            yield return new ValidationResult(
                "Quantity supports at most 3 decimal places.",
                [nameof(Quantity)]);
        }

        if (decimal.Round(BaselineUnitPrice, 4) != BaselineUnitPrice)
        {
            yield return new ValidationResult(
                "Baseline price supports at most 4 decimal places.",
                [nameof(BaselineUnitPrice)]);
        }
    }
}