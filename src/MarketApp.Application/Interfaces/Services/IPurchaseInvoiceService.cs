using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Purchases;

namespace MarketApp.Application.Interfaces.Services;

public interface IPurchaseInvoiceService
{
    Task<MarketApp.Application.Common.PagedResult<PurchaseInvoiceDto>> GetPageAsync(MarketApp.Application.DTOs.Sales.PageQuery page, CancellationToken ct = default);

    Task<PurchaseInvoiceResult> CreateAsync(
        CreatePurchaseInvoiceDto request,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceResult> PostAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}