using MarketApp.Domain.Entity.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class StockBalanceConfiguration
    : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances", table =>
        {
            table.HasCheckConstraint(
                "CK_StockBalances_Quantity_NonNegative",
                "\"Quantity\" >= 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(s => s.Version)
            .IsRowVersion();

        builder.HasIndex(s => new
        {
            s.ProductId,
            s.InventoryLocationId
        })
            .IsUnique()
            .HasDatabaseName("UX_StockBalances_Product_Location");

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.InventoryLocation)
            .WithMany()
            .HasForeignKey(s => s.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}