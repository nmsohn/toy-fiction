using Microsoft.EntityFrameworkCore;
using NarrativeEngine.Domain.Common;
using NarrativeEngine.Domain.Entities;
using NarrativeEngine.Infrastructure.Data.Configuration;

namespace NarrativeEngine.Infrastructure.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserAccessor currentUser) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterTrait> CharacterTraits => Set<CharacterTrait>();
    public DbSet<CharacterEmbedding> CharacterEmbeddings => Set<CharacterEmbedding>();
    public DbSet<EmotionScore> EmotionScores => Set<EmotionScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new ChapterConfiguration());
        modelBuilder.ApplyConfiguration(new CharacterConfiguration());
        modelBuilder.ApplyConfiguration(new CharacterTraitConfiguration());
        modelBuilder.ApplyConfiguration(new CharacterEmbeddingConfiguration());
        modelBuilder.ApplyConfiguration(new EmotionScoreConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var actor = currentUser.UserId?.ToString() ?? "system";

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                entry.Entity.DeletedBy = actor;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
