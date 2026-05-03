using BookEcom.Domain.Abstractions;

namespace BookEcom.Infrastructure.Data;

/// <summary>
/// EF-backed <see cref="IUnitOfWork"/>. Forwards to the request-scoped
/// <see cref="AppDbContext"/>. Registered Scoped in DI so it shares the
/// DbContext's lifetime — anything broader would be a captive dependency.
/// </summary>
public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct) =>
        new AppTransaction(await db.Database.BeginTransactionAsync(ct));
}
