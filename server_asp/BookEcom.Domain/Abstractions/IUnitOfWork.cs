namespace BookEcom.Domain.Abstractions;

/// <summary>
/// Transaction boundary for the Application layer. Repositories stage changes
/// (Add / Remove, tracked mutations); <see cref="SaveChangesAsync"/> flushes
/// everything staged across every repository in a single database
/// transaction. <see cref="BeginTransactionAsync"/> is only needed when an
/// operation mixes bulk SQL (e.g. ExecuteDelete) with change-tracker writes
/// — the single-commit case should prefer <see cref="SaveChangesAsync"/>.
///
/// Lives in Domain (rather than Application) so Infrastructure can implement
/// it without a project-reference flip; see Phase 5b for that follow-up.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct);
}
