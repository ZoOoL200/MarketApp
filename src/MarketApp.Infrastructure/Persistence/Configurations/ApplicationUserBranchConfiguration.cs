using MarketApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class ApplicationUserBranchConfiguration
    : IEntityTypeConfiguration<ApplicationUserBranch>
{
    public void Configure(EntityTypeBuilder<ApplicationUserBranch> builder)
    {
        builder.ToTable("UserBranchAssignments");

        builder.HasKey(x => new { x.UserId, x.BranchId });

        builder.HasIndex(x => x.BranchId);

        builder.HasOne(x => x.User)
            .WithMany(x => x.BranchAssignments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}