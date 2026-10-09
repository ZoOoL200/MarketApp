using MarketApp.Application.DTOs.Sales;
namespace MarketApp.Application.Interfaces.Services;
public interface IInventoryOperationsService
{
    Task<ReturnDto> ReturnAsync(Guid branchId, Guid saleId, Guid actorId, CreateReturnDto request, CancellationToken ct = default);
    Task<AdjustmentDto> AdjustAsync(Guid actorId, CreateAdjustmentDto request, CancellationToken ct = default);
}
