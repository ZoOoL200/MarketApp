using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
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
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}