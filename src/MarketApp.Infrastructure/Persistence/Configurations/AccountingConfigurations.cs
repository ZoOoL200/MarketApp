using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Sales;
using MarketApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace MarketApp.Infrastructure.Persistence.Configurations;
public sealed class StockCostHistoryConfiguration : IEntityTypeConfiguration<StockCostHistory>
{
    public void Configure(EntityTypeBuilder<StockCostHistory> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.AveragePurchaseUnitCost).HasPrecision(18, 6);
        b.Property(x => x.QuantityAtChange).HasPrecision(18, 3);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.RequestHash).HasMaxLength(64);
        b.HasIndex(x => x.ClientChangeId).IsUnique();
        b.HasIndex(x => new { x.ProductId, x.InventoryLocationId, x.EffectiveAtUtc, x.Id });
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("StockCostHistories", t => t.HasCheckConstraint("CK_StockCostHistory_Values", "\"AveragePurchaseUnitCost\" >= 0 AND \"QuantityAtChange\" >= 0"));
    }
}
public sealed class BranchExpenseConfiguration : IEntityTypeConfiguration<BranchExpense>
{
    public void Configure(EntityTypeBuilder<BranchExpense> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.Version).IsRowVersion();
        b.Property(x => x.Category).HasMaxLength(20).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.VoidReason).HasMaxLength(1000);
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.BranchId, x.ClientExpenseId }).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.OccurredAtUtc, x.Id });
        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.EmployeeUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("BranchExpenses", t => {
            t.HasCheckConstraint("CK_BranchExpense_Amount", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_BranchExpense_Category", "\"Category\" IN ('Salary','Other')");
            t.HasCheckConstraint("CK_BranchExpense_Void", """
                ("VoidedAtUtc" IS NULL AND "VoidedByUserId" IS NULL AND "VoidReason" IS NULL) OR
                ("VoidedAtUtc" IS NOT NULL AND "VoidedByUserId" IS NOT NULL AND "VoidReason" IS NOT NULL)
                """);
        });
    }
}
public sealed class SaleCostConfiguration : IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> b)
    {
        b.Property(x => x.PurchaseUnitCostSnapshot).HasPrecision(18, 6);
        b.Property(x => x.PurchaseCostRecordHash).HasMaxLength(64);
        b.Property(x => x.PurchaseCostReason).HasMaxLength(1000);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PurchaseCostRecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("SalesInvoiceLines", t => t.HasCheckConstraint("CK_SaleCost_Nonnegative", "\"PurchaseUnitCostSnapshot\" >= 0"));
    }
}
public sealed class BalanceCostConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> b)
    {
        b.Property(x => x.AveragePurchaseUnitCost).HasPrecision(18, 6);
        b.ToTable("StockBalances", t => t.HasCheckConstraint("CK_BalanceCost_Nonnegative", "\"AveragePurchaseUnitCost\" >= 0"));
    }
}
public sealed class TransferCostConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> b)
    {
        b.Property(x => x.PurchaseUnitCostSnapshot).HasPrecision(18, 6);
        b.ToTable("StockTransferLines", t => t.HasCheckConstraint("CK_TransferCost_Nonnegative", "\"PurchaseUnitCostSnapshot\" >= 0"));
    }
}
