using MarketApp.Domain.Entity.Sales;
using MarketApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public sealed class ManagerProfitShareConfiguration : IEntityTypeConfiguration<ManagerProfitShare>
{
    public void Configure(EntityTypeBuilder<ManagerProfitShare> b)
    {
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => new { x.BranchId, x.ClientChangeId }).IsUnique();
        b.HasKey(x => x.Id); b.Property(x => x.Percent).HasPrecision(7, 4);
        b.HasIndex(x => new { x.BranchId, x.EffectiveFromUtc }).IsUnique();
        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ManagerUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("ManagerProfitShares", t => t.HasCheckConstraint("CK_ManagerShare_Percent", "\"Percent\" BETWEEN 0 AND 100"));
    }
}
