using BookEcom.Domain.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookEcom.Infrastructure.Data;

/// <summary>
/// Thin pass-through over EF's <see cref="IDbContextTransaction"/> so the
/// Application layer can deal with transactions through
/// <see cref="IAppTransaction"/> without referencing EF types.
/// </summary>
internal sealed class AppTransaction(IDbContextTransaction inner) : IAppTransaction
{
    public Task CommitAsync(CancellationToken ct) => inner.CommitAsync(ct);

    public Task RollbackAsync(CancellationToken ct) => inner.RollbackAsync(ct);

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
