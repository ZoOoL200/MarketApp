using MarketApp.Application.DTOs.Sales;
namespace MarketApp.Application.Interfaces.Services;
public interface ISalesService
{
    Task<PostedSaleDto> SellAsync(Guid branchId, Guid actorId, CreateSaleDto request, CancellationToken ct = default);
    Task<PostedSaleDto> SyncAsync(Guid branchId, Guid actorId, SyncSaleDto submission, CancellationToken ct = default, Guid? reconciledBy = null);
}
