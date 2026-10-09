using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Entity.Purchasing;
using MarketApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    public DbSet<MarketApp.Domain.Entity.Sales.SalesReturn> SalesReturns => Set<MarketApp.Domain.Entity.Sales.SalesReturn>();
    public DbSet<MarketApp.Domain.Entity.Sales.SalesReturnLine> SalesReturnLines => Set<MarketApp.Domain.Entity.Sales.SalesReturnLine>();
    public DbSet<MarketApp.Domain.Entity.Sales.StockAdjustment> StockAdjustments => Set<MarketApp.Domain.Entity.Sales.StockAdjustment>();
    public DbSet<MarketApp.Domain.Entity.Sales.ManagerProfitShare> ManagerProfitShares => Set<MarketApp.Domain.Entity.Sales.ManagerProfitShare>();
    public DbSet<MarketApp.Domain.Entity.Sales.SalesInvoice> SalesInvoices => Set<MarketApp.Domain.Entity.Sales.SalesInvoice>();
    public DbSet<MarketApp.Domain.Entity.Sales.SalesInvoiceLine> SalesInvoiceLines => Set<MarketApp.Domain.Entity.Sales.SalesInvoiceLine>();
    public DbSet<MarketApp.Domain.Entity.Inventory.StockCostHistory> StockCostHistories => Set<MarketApp.Domain.Entity.Inventory.StockCostHistory>();
    public DbSet<MarketApp.Domain.Entity.Sales.BranchExpense> BranchExpenses => Set<MarketApp.Domain.Entity.Sales.BranchExpense>();
    // Add DbSet properties for your entities
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductPhoto> ProductPhotos => Set<ProductPhoto>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryLocation> InventoryLocations=> Set<InventoryLocation>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<PurchaseInvoice> PurchaseInvoices=> Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines=> Set<PurchaseInvoiceLine>();
    public DbSet<StockMovement> StockMovements=> Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers=> Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines=> Set<StockTransferLine>();
    public DbSet<BranchProductPrice> BranchProductPrices=> Set<BranchProductPrice>();
    public DbSet<BranchProductPriceHistory> BranchProductPriceHistories=> Set<BranchProductPriceHistory>();
    public DbSet<ApplicationUserBranch> UserBranchAssignments => Set<ApplicationUserBranch>();
    public DbSet<AuthSession> AuthSessions=> Set<AuthSession>();
    public DbSet<RefreshToken> RefreshTokens=> Set<RefreshToken>();


    // Add more DbSet properties for other entities as needed
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }


}
