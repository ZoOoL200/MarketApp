using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Inventory;
namespace MarketApp.Application.Services;

// Purchase valuation is separate from the stakeholder's editable baseline selling price.
public static class StockCostAccounting
{
    public static async Task ReceiveAsync(IUnitOfWork work, StockBalance balance, decimal quantity,
        decimal? unitCost, string reason, CancellationToken ct)
    {
        var total = balance.Quantity + quantity;
        balance.AveragePurchaseUnitCost = balance.Quantity == 0
            ? (unitCost is decimal initial ? decimal.Round(initial, 6) : null)
            : balance.AveragePurchaseUnitCost is decimal old && unitCost is decimal incoming
                ? decimal.Round((old * balance.Quantity + incoming * quantity) / total, 6) : null;
        balance.Quantity = total;
        await RecordAsync(work, balance, reason, ct);
    }
    public static async Task RemoveAsync(IUnitOfWork work, StockBalance balance, decimal quantity,
        decimal? soldCost, CancellationToken ct)
    {
        var remaining = balance.Quantity - quantity;
        // A late offline sale removes its original value, not today's value.
        if (remaining > 0 && soldCost is decimal original && balance.AveragePurchaseUnitCost is decimal current && original != current)
        {
            var remainingValue = balance.Quantity * current - quantity * original;
            if (remainingValue < 0) throw new ConflictException("Inventory cost requires stakeholder reconciliation before posting this offline sale.");
            balance.AveragePurchaseUnitCost = decimal.Round(remainingValue / remaining, 6);
            balance.Quantity = remaining;
            await RecordAsync(work, balance, "Historical offline sale cost adjustment", ct);
        }
        else balance.Quantity = remaining;
    }
    public static Task RecordAsync(IUnitOfWork work, StockBalance balance, string reason, CancellationToken ct)
        => work.StockCostHistories.AddAsync(new StockCostHistory {
            ProductId = balance.ProductId, InventoryLocationId = balance.InventoryLocationId,
            AveragePurchaseUnitCost = balance.AveragePurchaseUnitCost, QuantityAtChange = balance.Quantity,
            Reason = reason }, ct);
}
