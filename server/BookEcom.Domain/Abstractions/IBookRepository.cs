using BookEcom.Domain.Entities;

namespace BookEcom.Domain.Abstractions;

/// <summary>
/// Persistence contract for the <see cref="Book"/> aggregate. Read methods
/// return untracked entities (EF change tracker skipped) because callers are
/// going to project them straight into a DTO. <see cref="FindForUpdateAsync"/>
/// returns a tracked entity because the caller is about to mutate it and
/// expects the subsequent <see cref="IUnitOfWork.SaveChangesAsync"/> to see
/// those changes. The name expresses intent so callers don't have to reason
/// about tracking flags.
/// </summary>
public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct);

    Task<Book?> GetByIdAsync(int id, CancellationToken ct);

    Task<Book?> FindForUpdateAsync(int id, CancellationToken ct);

    void Add(Book book);

    void Remove(Book book);
}
