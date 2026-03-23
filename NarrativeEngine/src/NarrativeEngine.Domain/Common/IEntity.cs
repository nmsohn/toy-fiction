namespace NarrativeEngine.Domain.Common;

public interface IEntity
{
    long Id { get; set; }
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime ModifiedAt { get; set; }
    string? ModifiedBy { get; set; }
}