using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    // Add DbSet properties for your entities
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryLocation> InventoryLocations=> Set<InventoryLocation>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<PurchaseInvoice> PurchaseInvoices=> Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines=> Set<PurchaseInvoiceLine>();
    public DbSet<StockMovement> StockMovements=> Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers=> Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines=> Set<StockTransferLine>();


    // Add more DbSet properties for other entities as needed
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }


}
