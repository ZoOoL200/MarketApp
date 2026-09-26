using MarketApp.Application.DTOs.Suppliers;

namespace MarketApp.Application.Common.Results;

public enum SupplierSaveStatus
{
    Success,
    NotFound,
    DuplicateCode
}

public record SupplierSaveResult(
    SupplierSaveStatus Status,
    SupplierDto? Supplier = null);