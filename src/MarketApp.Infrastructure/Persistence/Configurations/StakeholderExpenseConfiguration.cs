using MarketApp.Domain.Entity.Accounting;
using MarketApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace MarketApp.Infrastructure.Persistence.Configurations;
public sealed class StakeholderExpenseConfiguration : IEntityTypeConfiguration<StakeholderExpense>
{
    public void Configure(EntityTypeBuilder<StakeholderExpense> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.Version).IsRowVersion();
        b.Property(x => x.Category).HasMaxLength(20).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.PaidTo).HasMaxLength(200);
        b.Property(x => x.ReferenceNumber).HasMaxLength(100);
        b.Property(x => x.VoidReason).HasMaxLength(1000);
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.ClientExpenseId).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.OccurredAtUtc, x.Id });
        b.HasIndex(x => new { x.OccurredAtUtc, x.Id });
        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("StakeholderExpenses", t => {
            t.HasCheckConstraint("CK_StakeholderExpenses_Amount", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_StakeholderExpenses_Category", "\"Category\" IN ('Rent', 'Other')");
            t.HasCheckConstraint("CK_StakeholderExpenses_Void", """
                ("VoidedAtUtc" IS NULL AND "VoidedByUserId" IS NULL AND "VoidReason" IS NULL) OR
                ("VoidedAtUtc" IS NOT NULL AND "VoidedByUserId" IS NOT NULL AND "VoidReason" IS NOT NULL)
                """);
        });
    }
}
