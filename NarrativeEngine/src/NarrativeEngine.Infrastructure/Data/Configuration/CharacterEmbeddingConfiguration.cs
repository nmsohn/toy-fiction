using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NarrativeEngine.Domain.Entities;

namespace NarrativeEngine.Infrastructure.Data.Configuration;

public class CharacterEmbeddingConfiguration : IEntityTypeConfiguration<CharacterEmbedding>
{
    public void Configure(EntityTypeBuilder<CharacterEmbedding> builder)
    {
        builder.ToTable("character_embeddings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(e => e.Embedding)
            .IsRequired()
            .HasColumnType("vector(1536)");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(e => e.CharacterId)
            .HasDatabaseName("ux_character_embeddings_character_id")
            .IsUnique();
    }
}
