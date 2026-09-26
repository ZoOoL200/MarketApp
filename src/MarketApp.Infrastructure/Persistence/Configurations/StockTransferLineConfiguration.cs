using MarketApp.Domain.Entity.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class StockTransferLineConfiguration
    : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> builder)
    {
        builder.ToTable("StockTransferLines", table =>
        {
            table.HasCheckConstraint(
                "CK_StockTransferLines_LineNumber",
                "\"LineNumber\" > 0");

            table.HasCheckConstraint(
                "CK_StockTransferLines_Quantity",
                "\"Quantity\" > 0");

            table.HasCheckConstraint(
                "CK_StockTransferLines_BaselineUnitPrice",
                "\"BaselineUnitPrice\" >= 0");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(l => l.BaselineUnitPrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.HasIndex(l => new
        {
            l.StockTransferId,
            l.LineNumber
        })
            .IsUnique()
            .HasDatabaseName("UX_StockTransferLines_Transfer_Line");

        builder.HasOne(l => l.StockTransfer)
            .WithMany(t => t.Lines)
            .HasForeignKey(l => l.StockTransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}