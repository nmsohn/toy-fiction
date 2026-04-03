using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NarrativeEngine.Domain.Entities;

namespace NarrativeEngine.Infrastructure.Data.Configuration;

public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("characters");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(c => c.ModifiedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasQueryFilter(c => !c.IsDeleted);
        
        builder.HasIndex(c => c.ProjectId)
            .HasDatabaseName("ix_characters_projectId")
            .HasFilter("\"is_deleted\" = false");

        builder.HasMany(c => c.Traits)
            .WithOne(t => t.Character)
            .HasForeignKey(t => t.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Embedding)
            .WithOne(e => e.Character)
            .HasForeignKey<CharacterEmbedding>(e => e.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
