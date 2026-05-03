namespace BookEcom.Domain.Abstractions;

/// <summary>
/// Transaction handle surfaced to Application services without leaking EF's
/// <c>IDbContextTransaction</c>. The concrete implementation in Infrastructure
/// wraps the real EF transaction; callers Commit / Rollback / Dispose through
/// this interface so Application stays ORM-agnostic.
/// </summary>
public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
