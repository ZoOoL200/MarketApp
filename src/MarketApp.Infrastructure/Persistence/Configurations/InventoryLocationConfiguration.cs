using MarketApp.Domain.Entity.Main;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class InventoryLocationConfiguration
    : IEntityTypeConfiguration<InventoryLocation>
{
    public void Configure(
        EntityTypeBuilder<InventoryLocation> builder)
    {
        builder.ToTable("InventoryLocations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(l => l.Code)
            .IsUnique();

        builder.HasOne(l => l.Branch)
            .WithMany()
            .HasForeignKey(l => l.BranchId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}