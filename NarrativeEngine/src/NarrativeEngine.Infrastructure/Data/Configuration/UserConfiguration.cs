using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NarrativeEngine.Domain.Entities;

namespace NarrativeEngine.Infrastructure.Data.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Email)
            .HasDatabaseName("ux_users_email_active")
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        builder.Property(u => u.Email)
            .HasConversion(
                v => v.ToLowerInvariant(),
                v => v
            );

        builder.Property(u => u.PasswordHash);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.ModifiedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.ModifiedBy)
            .HasMaxLength(100);

        // Soft delete 
        builder.HasQueryFilter(u => !u.IsDeleted);

        // 관계 설정 (1:N - RefreshTokens)
        builder
            .HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 관계 설정 (1:1 - UserSettings)
        builder
            .HasOne(u => u.Settings)
            .WithOne(s => s.User)
            .HasForeignKey<UserSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}