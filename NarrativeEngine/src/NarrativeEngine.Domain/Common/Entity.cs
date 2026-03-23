namespace NarrativeEngine.Domain.Common;

public abstract class Entity : IEntity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}