using Microsoft.EntityFrameworkCore;

namespace NarrativeEngine.Api.Data;

/// <summary>
/// EF Core DbContext for NarrativeEngine.
/// DbSets and Fluent API configuration will be added in Tasks 2.1 and 2.3.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
