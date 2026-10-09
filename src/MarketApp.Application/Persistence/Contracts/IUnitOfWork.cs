using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Entity.Purchasing;

namespace MarketApp.Application.Persistence.Contracts;

public interface IUnitOfWork
{
    IGeneralRepository<Category> Categories { get; }

    IGeneralRepository<Product> Products { get; }
    IGeneralRepository<Branch> Branches { get; }
    IGeneralRepository<Supplier> Suppliers { get; }
    IGeneralRepository<InventoryLocation> InventoryLocations { get; }
    IGeneralRepository<StockBalance> StockBalances { get; }
    IGeneralRepository<PurchaseInvoice> PurchaseInvoices { get; }
    IGeneralRepository<StockMovement> StockMovements { get; }
    IGeneralRepository<StockTransfer> StockTransfers { get; }
    IGeneralRepository<BranchProductPrice> BranchProductPrices { get; }
    IGeneralRepository<BranchProductPriceHistory>BranchProductPriceHistories { get; }
    IGeneralRepository<MarketApp.Domain.Entity.Sales.SalesReturn> SalesReturns { get; }
    IGeneralRepository<MarketApp.Domain.Entity.Sales.StockAdjustment> StockAdjustments { get; }
    IGeneralRepository<MarketApp.Domain.Entity.Sales.ManagerProfitShare> ManagerProfitShares { get; }
    IGeneralRepository<MarketApp.Domain.Entity.Sales.SalesInvoice> SalesInvoices { get; }
    IGeneralRepository<MarketApp.Domain.Entity.Inventory.StockCostHistory> StockCostHistories { get; }
    IGeneralRepository<MarketApp.Domain.Entity.Sales.BranchExpense> BranchExpenses { get; }
    IGeneralRepository<MarketApp.Domain.Entity.Accounting.StakeholderExpense> StakeholderExpenses { get; }
    Task<IUnitOfWorkTransaction> BeginSerializableAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}