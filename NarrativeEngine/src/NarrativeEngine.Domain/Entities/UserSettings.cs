namespace NarrativeEngine.Domain.Entities;

public class UserSettings
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
}
