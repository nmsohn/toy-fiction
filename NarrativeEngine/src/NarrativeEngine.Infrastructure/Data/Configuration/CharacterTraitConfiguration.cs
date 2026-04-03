using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NarrativeEngine.Domain.Entities;
using NarrativeEngine.Domain.Enums;

namespace NarrativeEngine.Infrastructure.Data.Configuration;

public class CharacterTraitConfiguration : IEntityTypeConfiguration<CharacterTrait>
{
    public void Configure(EntityTypeBuilder<CharacterTrait> builder)
    {
        builder.ToTable("character_traits");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityAlwaysColumn();

        builder.Property(t => t.Key)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(t => t.Label)
            .IsRequired()
            .HasMaxLength(255);
        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
        builder.Property(t => t.Value)
            .HasColumnType("text");
        builder.Property(t => t.Options)
            .HasColumnType("jsonb");
        builder.Property(t => t.OrderIndex)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(t => t.ModifiedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(t => new { t.CharacterId, t.Key })
            .HasDatabaseName("ux_character_traits_character_key")
            .IsUnique();
    }
}
