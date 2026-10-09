using MarketApp.Domain.Entity.Sales;
using MarketApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.Number).IsUnique();
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.ManagerSharePercentSnapshot).HasPrecision(7, 4);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ManagerUserIdSnapshot).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("SalesInvoices", t => t.HasCheckConstraint("CK_SalesInvoices_ManagerShare", """
            "ManagerSharePercentSnapshot" BETWEEN 0 AND 100 AND
            ("ManagerUserIdSnapshot" IS NOT NULL OR "ManagerSharePercentSnapshot" = 0)
            """));
        b.HasIndex(x => new { x.BranchId, x.ClientSaleId }).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.SoldAtUtc, x.Id });
        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.InventoryLocation).WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SoldByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReconciledByUserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("SalesInvoices", t => t.HasCheckConstraint("CK_SalesInvoices_Reconciliation", """
            ("ReconciledByUserId" IS NULL AND "ReconciledAtUtc" IS NULL) OR
            ("ReconciledByUserId" IS NOT NULL AND "ReconciledAtUtc" IS NOT NULL AND "IsOffline")
            """));
    }
}
public sealed class SalesInvoiceLineConfiguration : IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        b.Property(x => x.ProductSkuSnapshot).HasMaxLength(64).IsRequired();
        b.Property(x => x.Quantity).HasPrecision(18, 3);
        b.Property(x => x.ReturnedQuantity).HasPrecision(18, 3);
        b.Property(x => x.Version).IsRowVersion();
        b.Property(x => x.BaselineUnitPriceSnapshot).HasPrecision(18, 4);
        b.Property(x => x.MinimumSellingPriceSnapshot).HasPrecision(18, 4);
        b.Property(x => x.SellingUnitPrice).HasPrecision(18, 4);
        b.HasIndex(x => new { x.SalesInvoiceId, x.LineNumber }).IsUnique();
        b.HasIndex(x => new { x.SalesInvoiceId, x.ProductId }).IsUnique();
        b.HasOne(x => x.SalesInvoice).WithMany(x => x.Lines).HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("SalesInvoiceLines", t =>
        {
            t.HasCheckConstraint("CK_SalesLines_Quantity", "\"Quantity\" > 0 AND \"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\"");
            t.HasCheckConstraint("CK_SalesLines_Price", "\"BaselineUnitPriceSnapshot\" >= 0 AND \"MinimumSellingPriceSnapshot\" >= 0 AND \"SellingUnitPrice\" >= \"MinimumSellingPriceSnapshot\"");
            t.HasCheckConstraint("CK_SalesLines_Sequence", "\"LineNumber\" > 0 AND \"PriceRevisionSnapshot\" > 0");
        });
    }
}
