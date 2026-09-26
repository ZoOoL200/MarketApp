namespace MarketApp.Application.DTOs.Suppliers;

public record SupplierDto(
    Guid Id,
    string Name,
    string Code,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive);