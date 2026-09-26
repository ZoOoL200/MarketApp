namespace MarketApp.Application.DTOs.Stock;

public record StockBalanceDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Sku,
    Guid InventoryLocationId,
    string InventoryLocationName,
    string InventoryLocationCode,
    decimal Quantity);