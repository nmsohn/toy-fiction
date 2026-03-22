using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NarrativeEngine.Domain.Entities;

namespace NarrativeEngine.Infrastructure.Data.Configuration;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .UseIdentityAlwaysColumn()
            .HasAnnotation("Relational:SequenceOptions", "CACHE 100"); //ALTER SEQUENCE refresh_tokens_id_seq CACHE 100;
                                                                       // Caching sequence value to reduce race condition for lock
        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);
        
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);
        builder.Property(t => t.RevokeReason).HasMaxLength(50);

        builder.Property(t => t.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(t => t.RevokedAt)
            .HasColumnType("timestamp with time zone");
    }
}
