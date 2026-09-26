using MarketApp.Domain.Entity.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class StockTransferConfiguration
    : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers", table =>
        {
            table.HasCheckConstraint(
                "CK_StockTransfers_DifferentLocations",
                "\"SourceLocationId\" <> \"DestinationLocationId\"");

            table.HasCheckConstraint(
                "CK_StockTransfers_Status",
                "\"Status\" IN (1, 2, 3, 4)");

            table.HasCheckConstraint(
                "CK_StockTransfers_StatusTimestamps",
                "(" +
                "\"Status\" IN (1, 4) " +
                "AND \"ShippedAtUtc\" IS NULL " +
                "AND \"ReceivedAtUtc\" IS NULL" +
                ") OR (" +
                "\"Status\" = 2 " +
                "AND \"ShippedAtUtc\" IS NOT NULL " +
                "AND \"ReceivedAtUtc\" IS NULL" +
                ") OR (" +
                "\"Status\" = 3 " +
                "AND \"ShippedAtUtc\" IS NOT NULL " +
                "AND \"ReceivedAtUtc\" IS NOT NULL " +
                "AND \"ReceivedAtUtc\" >= \"ShippedAtUtc\"" +
                ")");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Number)
            .IsRequired()
            .HasMaxLength(40);

        builder.HasIndex(t => t.Number)
            .IsUnique();

        builder.Property(t => t.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.Property(t => t.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.ShippedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.ReceivedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.Version)
            .IsRowVersion();

        builder.HasOne(t => t.SourceLocation)
            .WithMany()
            .HasForeignKey(t => t.SourceLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DestinationLocation)
            .WithMany()
            .HasForeignKey(t => t.DestinationLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}