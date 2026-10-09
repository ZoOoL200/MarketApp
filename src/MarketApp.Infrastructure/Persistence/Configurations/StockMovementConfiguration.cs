using MarketApp.Domain.Entity.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration
    : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
        {
            table.HasCheckConstraint("CK_StockMovements_TypeAndQuantity",
                "(\"MovementType\" IN (1, 3, 4, 6) AND \"QuantityChange\" > 0) OR " +
                "(\"MovementType\" IN (2, 5) AND \"QuantityChange\" < 0) OR (\"MovementType\" = 7 AND \"QuantityChange\" <> 0)");
            table.HasCheckConstraint("CK_StockMovements_Source", """
                num_nonnulls("PurchaseInvoiceLineId", "StockTransferLineId", "SalesInvoiceLineId", "SalesReturnLineId", "StockAdjustmentId") = 1 AND (
                ("MovementType" = 1 AND "PurchaseInvoiceLineId" IS NOT NULL)
                OR ("MovementType" IN (2, 3, 4) AND "StockTransferLineId" IS NOT NULL)
                OR ("MovementType" = 5 AND "SalesInvoiceLineId" IS NOT NULL)
                OR ("MovementType" = 6 AND "SalesReturnLineId" IS NOT NULL)
                OR ("MovementType" = 7 AND "StockAdjustmentId" IS NOT NULL))
                """);
        });

        builder.HasIndex(m => m.SalesReturnLineId).IsUnique().HasFilter("\"SalesReturnLineId\" IS NOT NULL");
        builder.HasOne(m => m.SalesReturnLine).WithMany().HasForeignKey(m => m.SalesReturnLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.StockAdjustmentId).IsUnique().HasFilter("\"StockAdjustmentId\" IS NOT NULL");
        builder.HasOne(m => m.StockAdjustment).WithMany().HasForeignKey(m => m.StockAdjustmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.SalesInvoiceLineId).IsUnique()
            .HasFilter("\"SalesInvoiceLineId\" IS NOT NULL")
            .HasDatabaseName("UX_StockMovements_SalesInvoiceLine");
        builder.HasOne(m => m.SalesInvoiceLine).WithMany()
            .HasForeignKey(m => m.SalesInvoiceLineId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.MovementType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.QuantityChange)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(m => m.OccurredAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(m => m.PurchaseInvoiceLineId)
            .IsUnique()
            .HasFilter("\"PurchaseInvoiceLineId\" IS NOT NULL")
            .HasDatabaseName("UX_StockMovements_PurchaseInvoiceLine");

        builder.HasIndex(m => new
        {
            m.StockTransferLineId,
            m.MovementType
        })
            .IsUnique()
            .HasFilter("\"StockTransferLineId\" IS NOT NULL")
            .HasDatabaseName("UX_StockMovements_TransferLine_Type");

        builder.HasIndex(m => new
        {
            m.ProductId,
            m.InventoryLocationId,
            m.OccurredAtUtc,
            m.Id
        })
            .HasDatabaseName("IX_StockMovements_Product_Location_History");

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.InventoryLocation)
            .WithMany()
            .HasForeignKey(m => m.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.PurchaseInvoiceLine)
            .WithMany()
            .HasForeignKey(m => m.PurchaseInvoiceLineId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.StockTransferLine)
            .WithMany()
            .HasForeignKey(m => m.StockTransferLineId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}