using BookEcom.Application.Books;
using BookEcom.Domain.Common.Results;
using BookEcom.Application.Dtos.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BooksController(IBookService booksService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<BookResponse>> GetAll(CancellationToken ct) =>
        await booksService.GetAllAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookResponse>> GetById(int id, CancellationToken ct) =>
        (await booksService.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<BookResponse>> Create(CreateBookRequest req, CancellationToken ct) =>
        (await booksService.CreateAsync(req, ct)).ToCreatedAtAction(this, nameof(GetById), book => new { id = book.Id });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBookRequest req, CancellationToken ct) =>
        (await booksService.UpdateAsync(id, req, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        (await booksService.DeleteAsync(id, ct)).ToActionResult();
}
