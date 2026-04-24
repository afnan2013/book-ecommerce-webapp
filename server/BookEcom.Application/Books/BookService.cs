using BookEcom.Application.Dtos.Books;
using BookEcom.Domain.Abstractions;
using BookEcom.Domain.Common.Results;
using BookEcom.Domain.Entities;

namespace BookEcom.Application.Books;

public class BookService(
    IBookRepository bookRepo,
    IUnitOfWork uow,
    ILogger<BookService> logger) : IBookService
{
    public async Task<IReadOnlyList<BookResponse>> GetAllAsync(CancellationToken ct)
    {
        var books = await bookRepo.GetAllAsync(ct);
        logger.LogInformation("Books.GetAll — returning {Count} books", books.Count);
        return books.Select(ToResponse).ToList();
    }

    public async Task<Result<BookResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var book = await bookRepo.GetByIdAsync(id, ct);
        if (book is null) return Result<BookResponse>.NotFound($"Book {id} not found.");
        return ToResponse(book);
    }

    public async Task<Result<BookResponse>> CreateAsync(CreateBookRequest req, CancellationToken ct)
    {
        var created = Book.Create(req.Title, req.Author, req.Price);
        if (created.IsFailure) return created.Error!;

        var book = created.Value!;
        bookRepo.Add(book);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Books.Create — created book {Id}", book.Id);
        return ToResponse(book);
    }

    public async Task<Result> UpdateAsync(int id, UpdateBookRequest req, CancellationToken ct)
    {
        var book = await bookRepo.FindForUpdateAsync(id, ct);
        if (book is null) return Result.NotFound($"Book {id} not found.");

        var updated = book.Update(req.Title, req.Author, req.Price);
        if (updated.IsFailure) return updated;

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Books.Update — updated book {Id}", id);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var book = await bookRepo.FindForUpdateAsync(id, ct);
        if (book is null) return Result.NotFound($"Book {id} not found.");

        bookRepo.Remove(book);
        await uow.SaveChangesAsync(ct);
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
