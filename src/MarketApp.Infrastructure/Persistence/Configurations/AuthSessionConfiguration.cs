using MarketApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class AuthSessionConfiguration
    : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("AuthSessions", table =>
        {
            table.HasCheckConstraint(
                "CK_AuthSessions_Expiry",
                "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");

            table.HasCheckConstraint(
                "CK_AuthSessions_LastRefresh",
                "\"LastRefreshedAtUtc\" IS NULL OR " +
                "\"LastRefreshedAtUtc\" >= \"CreatedAtUtc\"");

            table.HasCheckConstraint(
                "CK_AuthSessions_Revocation",
                "\"RevokedAtUtc\" IS NULL OR " +
                "\"RevokedAtUtc\" >= \"CreatedAtUtc\"");

            table.HasCheckConstraint(
                "CK_AuthSessions_SecurityStamp",
                "length(btrim(\"SecurityStamp\")) > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SecurityStamp)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.LastRefreshedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Version)
            .IsRowVersion();

        builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc })
            .HasDatabaseName("IX_AuthSessions_User_Expiry");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_AuthSessions_Expiry");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}