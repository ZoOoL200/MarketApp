using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Entity.Purchasing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MarketApp.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IGeneralRepository<Category> Categories { get; }

    public IGeneralRepository<Product> Products { get; }
    public IGeneralRepository<Branch> Branches { get; }
    public IGeneralRepository<Supplier> Suppliers { get; }
    public IGeneralRepository<InventoryLocation> InventoryLocations { get; }
    public IGeneralRepository<StockBalance> StockBalances { get; }
    public IGeneralRepository<PurchaseInvoice> PurchaseInvoices{ get; }
    public IGeneralRepository<StockMovement> StockMovements { get; }
    public IGeneralRepository<StockTransfer> StockTransfers { get; }
    public IGeneralRepository<BranchProductPrice> BranchProductPrices{ get;}
    public IGeneralRepository<BranchProductPriceHistory> BranchProductPriceHistories{get;}

    public UnitOfWork(
        AppDbContext context,
        IGeneralRepository<Category> categories,
        IGeneralRepository<Product> products,
        IGeneralRepository<Branch> branches,
        IGeneralRepository<Supplier> suppliers,
        IGeneralRepository<InventoryLocation> inventoryLocations,
        IGeneralRepository<StockBalance> stockBalances,
        IGeneralRepository<PurchaseInvoice> purchaseInvoices,
        IGeneralRepository<StockMovement> stockMovements,
        IGeneralRepository<StockTransfer> stockTransfers,
        IGeneralRepository<BranchProductPrice> branchProductPrices,
        IGeneralRepository<BranchProductPriceHistory> branchProductPriceHistories)
    {
        _context = context;
        Categories = categories;
        Products = products;
        Branches = branches;
        Suppliers = suppliers;
        InventoryLocations = inventoryLocations;
        StockBalances = stockBalances;
        PurchaseInvoices = purchaseInvoices;
        StockMovements = stockMovements;
        StockTransfers = stockTransfers;
        BranchProductPrices = branchProductPrices;
        BranchProductPriceHistories = branchProductPriceHistories;
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(
                "The record changed or was deleted. " +
                "Refresh the data and try again.",
                exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            var postgresException =
                (PostgresException)exception.InnerException!;

            var message = postgresException.ConstraintName switch
            {
                "IX_Products_Sku" =>
                    "Another product already uses this SKU.",

                "IX_Branches_Code" =>
                    "Another branch already uses this code.",

                "IX_Suppliers_Code" =>
                    "Another supplier already uses this code.",

                "IX_InventoryLocations_Code" =>
                    "Another inventory location already uses this code.",

                "UX_StockBalances_Product_Location" =>
                    "A stock balance already exists for this product and location.",

                "IX_PurchaseInvoices_Number" =>
                    "A purchase invoice already uses this number.",

                "UX_PurchaseInvoiceLines_Invoice_Line" =>
                    "Line numbers must be unique within a purchase invoice.",

                "UX_StockMovements_PurchaseInvoiceLine" =>
                    "A stock receipt already exists for this purchase invoice line.",

                "IX_StockTransfers_Number" =>
                    "A stock transfer already uses this number.",

                "UX_StockTransferLines_Transfer_Line" =>
                    "Line numbers must be unique within a stock transfer.",

                "UX_StockMovements_TransferLine_Type" =>
                    "This transfer line already has a movement of this type.",

                "UX_BranchProductPrices_Branch_Product" =>
                    "Pricing already exists for this product and branch. " +
                    "Refresh and try again.",

                "UX_BranchPriceHistory_Price_Revision" =>
                    "This price revision already exists. Refresh and try again.",

                _ =>
                    "A record with the same unique value already exists."
            };

            throw new ConflictException(message, exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ForeignKeyViolation
            })
        {
            throw new ConflictException(
                "This operation conflicts with related records. " +
                "A referenced record may no longer exist, or this " +
                "record may still be in use.",
                exception);
        }
    }
}