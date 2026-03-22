namespace NarrativeEngine.Domain.Common;

public interface ICurrentUserAccessor
{
    long? UserId { get; }
}
