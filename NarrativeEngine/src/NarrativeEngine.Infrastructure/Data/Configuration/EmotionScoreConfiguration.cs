using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NarrativeEngine.Domain.Entities;

namespace NarrativeEngine.Infrastructure.Data.Configuration;

public class EmotionScoreConfiguration : IEntityTypeConfiguration<EmotionScore>
{
    public void Configure(EntityTypeBuilder<EmotionScore> builder)
    {
        builder.ToTable("emotion_scores");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(e => e.ParagraphIndex)
            .IsRequired();
        builder.Property(e => e.Score)
            .HasColumnType("real");
        builder.Property(e => e.EmotionTag)
            .HasMaxLength(100);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // (chapter_id, paragraph_index) 유니크 인덱스
        builder.HasIndex(e => new { e.ChapterId, e.ParagraphIndex })
            .HasDatabaseName("ux_emotion_scores_chapter_paragraph")
            .IsUnique();
    }
}
