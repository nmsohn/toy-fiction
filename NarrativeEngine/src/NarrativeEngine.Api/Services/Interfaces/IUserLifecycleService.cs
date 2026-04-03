namespace NarrativeEngine.Api.Services.Interfaces;

public interface IUserLifecycleService
{
    /// <summary>
    /// 단일 트랜잭션으로 User → Projects → Chapters/Characters 연쇄 Soft Delete.
    /// </summary>
    Task SoftDeleteUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// 보존 기간이 지난 Soft Delete 데이터를 Hard Delete(Purge)한다.
    /// </summary>
    Task<int> PurgeDeletedDataAsync(TimeSpan retention, CancellationToken ct = default);
}
