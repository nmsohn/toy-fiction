using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NarrativeEngine.Domain.Entities;

namespace NarrativeEngine.Infrastructure.Data.Configuration;

public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.ToTable("chapters");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(c => c.Content)
            .IsRequired()
            .HasColumnType("text");
        builder.Property(c => c.Summary)
            .HasColumnType("text");
        builder.Property(c => c.OrderIndex)
            .IsRequired();
        builder.Property(c => c.TokenCount)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(c => c.ModifiedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasQueryFilter(c => !c.IsDeleted);
        
        builder.HasIndex(c => new { c.ProjectId, c.OrderIndex })
            .HasDatabaseName("ix_chapters_projectId_orderIndex")
            .HasFilter("\"is_deleted\" = false");

        builder.HasMany(c => c.EmotionScores)
            .WithOne(e => e.Chapter)
            .HasForeignKey(e => e.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
