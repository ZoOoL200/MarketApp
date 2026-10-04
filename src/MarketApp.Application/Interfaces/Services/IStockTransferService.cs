using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.StockTransfers;

namespace MarketApp.Application.Interfaces.Services;

public interface IStockTransferService
{
    Task<StockTransferResult> CreateAsync(
        CreateStockTransferDto request,
        CancellationToken cancellationToken = default);

    Task<StockTransferDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StockTransferResult> ShipAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StockTransferResult> ReceiveAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StockTransferResult> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<StockTransferResult> RequestReturnAsync(
        Guid id,
        RequestStockTransferReturnDto request,
        CancellationToken cancellationToken = default);

    Task<StockTransferResult> ConfirmReturnAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}