using MarketApp.Domain.Entity.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class PurchaseInvoiceLineConfiguration
    : IEntityTypeConfiguration<PurchaseInvoiceLine>
{
    public void Configure(
        EntityTypeBuilder<PurchaseInvoiceLine> builder)
    {
        builder.ToTable("PurchaseInvoiceLines", table =>
        {
            table.HasCheckConstraint(
                "CK_PurchaseInvoiceLines_LineNumber",
                "\"LineNumber\" > 0");

            table.HasCheckConstraint(
                "CK_PurchaseInvoiceLines_Quantity",
                "\"Quantity\" > 0");

            table.HasCheckConstraint(
                "CK_PurchaseInvoiceLines_UnitCost",
                "\"UnitCost\" >= 0");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(l => l.UnitCost)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.HasIndex(l => new
        {
            l.PurchaseInvoiceId,
            l.LineNumber
        })
            .IsUnique()
            .HasDatabaseName("UX_PurchaseInvoiceLines_Invoice_Line");

        builder.HasOne(l => l.PurchaseInvoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}