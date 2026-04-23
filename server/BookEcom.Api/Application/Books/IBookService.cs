using BookEcom.Api.Dtos.Books;
using BookEcom.Domain.Common.Results;

namespace BookEcom.Api.Application.Books;

/// <summary>
/// Application-layer use cases for the Book aggregate. Returns <see cref="Result"/>
/// / <see cref="Result{T}"/> so the caller (the controller) can translate
/// outcomes into HTTP without this service knowing anything about ASP.NET.
///
/// Note: <see cref="GetAllAsync"/> deliberately returns a bare list — an empty
/// result is not an error, so wrapping it in <see cref="Result{T}"/> would add
/// ceremony without meaning. Result is reserved for operations that can fail
/// in an expected way.
/// </summary>
public interface IBookService
{
    Task<IReadOnlyList<BookResponse>> GetAllAsync(CancellationToken ct);
    Task<Result<BookResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<BookResponse>> CreateAsync(CreateBookRequest req, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateBookRequest req, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
