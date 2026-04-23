using BookEcom.Application.Dtos.Books;
using BookEcom.Domain.Common.Results;
using BookEcom.Domain.Entities;
using BookEcom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Application.Books;

public class BookService(AppDbContext db, ILogger<BookService> logger) : IBookService
{
    public async Task<IReadOnlyList<BookResponse>> GetAllAsync(CancellationToken ct)
    {
        var books = await db.Books.AsNoTracking().ToListAsync(ct);
        logger.LogInformation("Books.GetAll — returning {Count} books", books.Count);
        return books.Select(ToResponse).ToList();
    }

    public async Task<Result<BookResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book is null) return Result<BookResponse>.NotFound($"Book {id} not found.");
        return ToResponse(book);
    }

    public async Task<Result<BookResponse>> CreateAsync(CreateBookRequest req, CancellationToken ct)
    {
        var created = Book.Create(req.Title, req.Author, req.Price);
        if (created.IsFailure) return created.Error!;

        var book = created.Value!;
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Books.Create — created book {Id}", book.Id);
        return ToResponse(book);
    }

    public async Task<Result> UpdateAsync(int id, UpdateBookRequest req, CancellationToken ct)
    {
        var book = await db.Books.FindAsync([id], ct);
        if (book is null) return Result.NotFound($"Book {id} not found.");

        var updated = book.Update(req.Title, req.Author, req.Price);
        if (updated.IsFailure) return updated;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Books.Update — updated book {Id}", id);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var book = await db.Books.FindAsync([id], ct);
        if (book is null) return Result.NotFound($"Book {id} not found.");

        db.Books.Remove(book);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Books.Delete — deleted book {Id}", id);
        return Result.Success();
    }

    private static BookResponse ToResponse(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        Price = book.Price,
    };
}
