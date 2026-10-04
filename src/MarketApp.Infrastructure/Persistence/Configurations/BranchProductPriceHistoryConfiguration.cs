using MarketApp.Domain.Entity.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class BranchProductPriceHistoryConfiguration
    : IEntityTypeConfiguration<BranchProductPriceHistory>
{
    public void Configure(
        EntityTypeBuilder<BranchProductPriceHistory> builder)
    {
        builder.ToTable("BranchProductPriceHistory", table =>
        {
            table.HasCheckConstraint(
                "CK_BranchPriceHistory_Revision",
                "\"Revision\" > 0");

            table.HasCheckConstraint(
                "CK_BranchPriceHistory_Prices",
                "(\"OldBaselineUnitPrice\" IS NULL OR " +
                "\"OldBaselineUnitPrice\" >= 0) AND " +
                "(\"NewBaselineUnitPrice\" IS NULL OR " +
                "\"NewBaselineUnitPrice\" >= 0) AND " +
                "(\"OldMinimumSellingPrice\" IS NULL OR " +
                "\"OldMinimumSellingPrice\" >= 0) AND " +
                "(\"NewMinimumSellingPrice\" IS NULL OR " +
                "\"NewMinimumSellingPrice\" >= 0)");

            table.HasCheckConstraint(
                "CK_BranchPriceHistory_HasNewPrice",
                "\"NewBaselineUnitPrice\" IS NOT NULL OR " +
                "\"NewMinimumSellingPrice\" IS NOT NULL");

            table.HasCheckConstraint(
                "CK_BranchPriceHistory_Reason",
                "length(btrim(\"Reason\")) > 0");

            table.HasCheckConstraint(
                "CK_BranchPriceHistory_Source",
                "(" +
                "\"ChangeType\" IN (1, 2) " +
                "AND \"StockTransferLineId\" IS NULL" +
                ") OR (" +
                "\"ChangeType\" = 3 " +
                "AND \"StockTransferLineId\" IS NOT NULL" +
                ")");
        });

        builder.HasKey(h => h.Id);

        builder.Property(h => h.OldBaselineUnitPrice)
            .HasPrecision(18, 4);

        builder.Property(h => h.NewBaselineUnitPrice)
            .HasPrecision(18, 4);

        builder.Property(h => h.OldMinimumSellingPrice)
            .HasPrecision(18, 4);

        builder.Property(h => h.NewMinimumSellingPrice)
            .HasPrecision(18, 4);

        builder.Property(h => h.ChangeType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(h => h.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(h => h.ChangedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(h => new
        {
            h.BranchProductPriceId,
            h.Revision
        })
            .IsUnique()
            .HasDatabaseName("UX_BranchPriceHistory_Price_Revision");

        builder.HasOne(h => h.BranchProductPrice)
            .WithMany()
            .HasForeignKey(h => h.BranchProductPriceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.StockTransferLine)
            .WithMany()
            .HasForeignKey(h => h.StockTransferLineId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}