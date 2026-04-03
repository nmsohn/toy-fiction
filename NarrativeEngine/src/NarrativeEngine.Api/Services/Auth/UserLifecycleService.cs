using Microsoft.EntityFrameworkCore;
using NarrativeEngine.Api.Services.Interfaces;
using NarrativeEngine.Common;
using NarrativeEngine.Infrastructure.Data;

namespace NarrativeEngine.Api.Services.Auth;

public class UserLifecycleService(AppDbContext db, IClock clock) : IUserLifecycleService
{
    public async Task SoftDeleteUserAsync(long userId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");
        db.Users.Remove(user);

        // TODO: Project/Chapter/Character entity 
        // var projects = await db.Projects.Where(p => p.UserId == userId).ToListAsync(ct);
        // foreach (var project in projects) { db.Projects.Remove(project); ... }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<int> PurgeDeletedDataAsync(TimeSpan retention, CancellationToken ct = default)
    {
        var threshold = clock.UtcNow - retention;

        return await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.IsDeleted && u.DeletedAt < threshold)
            .ExecuteDeleteAsync(ct);
    }
}
