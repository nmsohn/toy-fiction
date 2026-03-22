namespace NarrativeEngine.Domain.Entities;

public class UserSettings
{
    public long Id { get; set; }
    public bool MuseEnabled { get; set; } = true;
    public string MuseTrigger { get; set; } = "manual";
    public bool EmotionAnalysisAuto { get; set; } = false;
    public bool BgmEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public long UserId { get; set; }
    // Navigation property
    public User User { get; set; } = null!;
}
