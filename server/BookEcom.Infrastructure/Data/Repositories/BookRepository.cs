using BookEcom.Domain.Abstractions;
using BookEcom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Infrastructure.Data.Repositories;

public class BookRepository(AppDbContext db) : IBookRepository
{
    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct) =>
        await db.Books.AsNoTracking().ToListAsync(ct);

    public async Task<Book?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);

    // FindAsync first checks the change-tracker cache, then falls back to the
    // DB. That's what we want for the update/delete path — if the entity was
    // already loaded in this request, we get the tracked instance back.
    public async Task<Book?> FindForUpdateAsync(int id, CancellationToken ct) =>
        await db.Books.FindAsync([id], ct);

    public void Add(Book book) => db.Books.Add(book);

    public void Remove(Book book) => db.Books.Remove(book);
}
