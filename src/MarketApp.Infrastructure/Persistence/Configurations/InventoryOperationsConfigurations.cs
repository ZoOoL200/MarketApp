using MarketApp.Domain.Entity.Sales;
using MarketApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(40).IsRequired();
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.ClientReturnId }).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.CreatedAtUtc, x.Id });
        b.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.Quantity).HasPrecision(18, 3);
        b.HasIndex(x => new { x.SalesReturnId, x.SalesInvoiceLineId }).IsUnique();
        b.HasOne(x => x.SalesReturn).WithMany(x => x.Lines).HasForeignKey(x => x.SalesReturnId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SalesInvoiceLine).WithMany().HasForeignKey(x => x.SalesInvoiceLineId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("SalesReturnLines", t => t.HasCheckConstraint("CK_ReturnLines_Quantity", "\"Quantity\" > 0"));
    }
}
public sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.QuantityChange).HasPrecision(18, 3);
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => x.ClientAdjustmentId).IsUnique();
        b.HasIndex(x => new { x.CreatedAtUtc, x.Id });
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.InventoryLocation).WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("StockAdjustments", t => t.HasCheckConstraint("CK_Adjustments_Quantity", "\"QuantityChange\" <> 0"));
    }
}
