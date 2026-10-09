using System.ComponentModel.DataAnnotations;
using MarketApp.Application.DTOs.Products;

namespace MarketApp.Application.DTOs.Sales;

public sealed class CreateSaleDto
{
    public Guid ClientSaleId { get; set; }
    public Guid InventoryLocationId { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
    [Required, MinLength(1), MaxLength(100)] public List<CreateSaleLineDto> Lines { get; set; } = [];
}
public sealed class CreateSaleLineDto
{
    public Guid ProductId { get; set; }
    [Range(typeof(decimal), "0.001", "1000000")] public decimal Quantity { get; set; }
    [Range(typeof(decimal), "0", "1000000000")] public decimal SellingUnitPrice { get; set; }
    [Range(1, int.MaxValue)] public int PriceRevision { get; set; }
}
public sealed class PageQuery
{
    [Range(1, 1000000)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 25;
    [StringLength(200)] public string? Search { get; set; }
}
// Seller-safe DTOs deliberately contain no baseline, gross profit or manager share.
public record SaleLineDto(Guid Id, Guid ProductId, string ProductName, string Sku,
    decimal Quantity, decimal SellingUnitPrice, decimal Total)
{
    public decimal ReturnedQuantity { get; init; }
}
public record SaleDto(Guid Id, string Number, Guid ClientSaleId, Guid BranchId,
    Guid InventoryLocationId, Guid SoldByUserId, DateTime SoldAtUtc, DateTime CreatedAtUtc,
    string? Notes, decimal Total, IReadOnlyList<SaleLineDto> Lines)
{
    public bool IsOffline { get; init; }
    public Guid? ReconciledByUserId { get; init; }
    public DateTime? ReconciledAtUtc { get; init; }
}
public record PostedSaleDto(bool AlreadyProcessed, SaleDto Sale);
public record CatalogProductDto(Guid Id, string Name, string Sku,
    decimal? MinimumSellingPrice, int PriceRevision, IReadOnlyList<ProductPhotoDto> Photos);
public record CatalogStockDto(Guid ProductId, Guid InventoryLocationId, decimal Quantity);

public record CatalogLocationDto(Guid Id, string Name, string Code);
