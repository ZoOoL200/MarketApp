using MarketApp.Domain.Entity.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class PurchaseInvoiceConfiguration
    : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("PurchaseInvoices", table =>
        {
            table.HasCheckConstraint(
                "CK_PurchaseInvoices_Status",
                "\"Status\" IN (1, 2)");

            table.HasCheckConstraint(
                "CK_PurchaseInvoices_PostedAt",
                "(\"Status\" = 1 AND \"PostedAtUtc\" IS NULL) OR " +
                "(\"Status\" = 2 AND \"PostedAtUtc\" IS NOT NULL)");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number)
            .IsRequired()
            .HasMaxLength(40);

        builder.HasIndex(i => i.Number)
            .IsUnique();

        builder.Property(i => i.SupplierInvoiceNumber)
            .HasMaxLength(100);

        builder.Property(i => i.InvoiceDate)
            .HasColumnType("date");

        builder.Property(i => i.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.Notes)
            .HasMaxLength(1000);

        builder.Property(i => i.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(i => i.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(i => i.PostedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(i => i.Version)
            .IsRowVersion();

        builder.HasOne(i => i.Supplier)
            .WithMany()
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.InventoryLocation)
            .WithMany()
            .HasForeignKey(i => i.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}