using MarketApp.Application.DTOs.Accounting;
namespace MarketApp.Application.Interfaces.Services;
public interface IStakeholderExpenseService
{
    Task<StakeholderExpenseDto> CreateAsync(Guid actorId, CreateStakeholderExpenseDto request, CancellationToken ct);
    Task<StakeholderExpenseDto> VoidAsync(Guid expenseId, Guid actorId, VoidStakeholderExpenseDto request, CancellationToken ct);
}
