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
            table.HasCheckConstraint(
                "CK_StockMovements_TypeAndQuantity",
                "(\"MovementType\" IN (1, 3) " +
                "AND \"QuantityChange\" > 0) OR " +
                "(\"MovementType\" = 2 " +
                "AND \"QuantityChange\" < 0)");

            table.HasCheckConstraint(
                "CK_StockMovements_Source",
                "(" +
                "\"MovementType\" = 1 " +
                "AND \"PurchaseInvoiceLineId\" IS NOT NULL " +
                "AND \"StockTransferLineId\" IS NULL" +
                ") OR (" +
                "\"MovementType\" IN (2, 3) " +
                "AND \"PurchaseInvoiceLineId\" IS NULL " +
                "AND \"StockTransferLineId\" IS NOT NULL" +
                ")");
        });

        builder.HasKey(m => m.Id);

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