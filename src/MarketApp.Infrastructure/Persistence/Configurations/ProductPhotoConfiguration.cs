using MarketApp.Domain.Entity.Main;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class ProductPhotoConfiguration : IEntityTypeConfiguration<ProductPhoto>
{
    public void Configure(EntityTypeBuilder<ProductPhoto> builder)
    {
        builder.ToTable("ProductPhotos", table =>
        {
            table.HasCheckConstraint("CK_ProductPhotos_StorageKey",
                "length(btrim(\"StorageKey\")) > 0");
            table.HasCheckConstraint("CK_ProductPhotos_ContentType",
                "\"ContentType\" IN ('image/jpeg', 'image/png', 'image/webp')");
            table.HasCheckConstraint("CK_ProductPhotos_FileSizeBytes",
                "\"FileSizeBytes\" > 0");
            table.HasCheckConstraint("CK_ProductPhotos_ContentHash",
                "\"ContentHash\" ~ '^[0-9A-F]{64}$'");
            table.HasCheckConstraint("CK_ProductPhotos_SortOrder",
                "\"SortOrder\" >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.StorageKey).IsRequired().HasMaxLength(512);
        builder.Property(p => p.ContentType).IsRequired().HasMaxLength(32);
        builder.Property(p => p.ContentHash).IsRequired().HasMaxLength(64);
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.IsReady, p.DeletedAtUtc, p.CreatedAtUtc });

        builder.HasIndex(p => p.StorageKey)
            .IsUnique()
            .HasDatabaseName("UX_ProductPhotos_StorageKey");

        builder.HasIndex(p => new { p.ProductId, p.SortOrder, p.Id });

        builder.HasOne(p => p.Product)
            .WithMany(p => p.Photos)
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
