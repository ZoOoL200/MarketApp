using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.Purchases;

public class CreatePurchaseInvoiceDto : IValidatableObject
{
    public Guid SupplierId { get; set; }

    public Guid InventoryLocationId { get; set; }

    public DateOnly InvoiceDate { get; set; }

    [StringLength(100)]
    public string? SupplierInvoiceNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public List<CreatePurchaseInvoiceLineDto> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (SupplierId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Supplier is required.",
                [nameof(SupplierId)]);
        }

        if (InventoryLocationId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Inventory location is required.",
                [nameof(InventoryLocationId)]);
        }

        if (InvoiceDate == default ||
            InvoiceDate == DateOnly.MaxValue)
        {
            yield return new ValidationResult(
                "A valid invoice date is required.",
                [nameof(InvoiceDate)]);
        }

        if (Lines is not null && Lines.Any(line => line is null))
        {
            yield return new ValidationResult(
                "Invoice lines cannot contain null entries.",
                [nameof(Lines)]);
        }
    }
}

public class CreatePurchaseInvoiceLineDto : IValidatableObject
{
    public Guid ProductId { get; set; }

    [Range(typeof(decimal), "0.001", "1000000000")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal UnitCost { get; set; }

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

        if (decimal.Round(UnitCost, 4) != UnitCost)
        {
            yield return new ValidationResult(
                "Unit cost supports at most 4 decimal places.",
                [nameof(UnitCost)]);
        }
    }
}