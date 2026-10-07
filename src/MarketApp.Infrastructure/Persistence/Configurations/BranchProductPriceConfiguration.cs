using MarketApp.Domain.Entity.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class BranchProductPriceConfiguration
    : IEntityTypeConfiguration<BranchProductPrice>
{
    public void Configure(EntityTypeBuilder<BranchProductPrice> builder)
    {
        builder.ToTable("BranchProductPrices", table =>
        {
            table.HasCheckConstraint(
                "CK_BranchProductPrices_Baseline",
                "\"BaselineUnitPrice\" IS NULL OR " +
                "\"BaselineUnitPrice\" >= 0");

            table.HasCheckConstraint(
                "CK_BranchProductPrices_Minimum",
                "\"MinimumSellingPrice\" IS NULL OR " +
                "\"MinimumSellingPrice\" >= 0");

            table.HasCheckConstraint(
                "CK_BranchProductPrices_HasPrice",
                "\"BaselineUnitPrice\" IS NOT NULL OR " +
                "\"MinimumSellingPrice\" IS NOT NULL");

            table.HasCheckConstraint(
                "CK_BranchProductPrices_Revision",
                "\"Revision\" > 0");

            table.HasCheckConstraint(
                "CK_BranchProductPrices_BaselineRevision",
                "\"BaselineRevision\" >= 0 AND " +
                "\"BaselineRevision\" <= \"Revision\" AND " +
                "((\"BaselineUnitPrice\" IS NULL AND \"BaselineRevision\" = 0) OR " +
                "(\"BaselineUnitPrice\" IS NOT NULL AND \"BaselineRevision\" > 0))");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.BaselineUnitPrice)
            .HasPrecision(18, 4);

        builder.Property(p => p.MinimumSellingPrice)
            .HasPrecision(18, 4);

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.Version)
            .IsRowVersion();

        builder.HasIndex(p => new
        {
            p.BranchId,
            p.ProductId
        })
            .IsUnique()
            .HasDatabaseName("UX_BranchProductPrices_Branch_Product");

        builder.HasOne(p => p.Branch)
            .WithMany()
            .HasForeignKey(p => p.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
