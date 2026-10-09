using MarketApp.Application.DTOs.Accounting;
namespace MarketApp.Application.Interfaces.Services;
public interface IStakeholderExpenseQueries
{
    Task<StakeholderExpensePageDto> ListAsync(StakeholderExpenseQuery query, CancellationToken ct);
    Task<StakeholderExpenseDto> GetAsync(Guid expenseId, CancellationToken ct);
}
