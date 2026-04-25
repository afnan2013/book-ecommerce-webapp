using BookEcom.Application.Auth.Authorization;
using BookEcom.Application.Books;
using BookEcom.Application.Dtos.Books;
using BookEcom.Domain.Auth;
using BookEcom.Domain.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BooksController(IBookService booksService) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionNames.BooksRead)]
    public async Task<IReadOnlyList<BookResponse>> GetAll(CancellationToken ct) =>
        await booksService.GetAllAsync(ct);

    [HttpGet("{id:int}")]
    [HasPermission(PermissionNames.BooksRead)]
    public async Task<ActionResult<BookResponse>> GetById(int id, CancellationToken ct) =>
        (await booksService.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    [HasPermission(PermissionNames.BooksCreate)]
    public async Task<ActionResult<BookResponse>> Create(CreateBookRequest req, CancellationToken ct) =>
        (await booksService.CreateAsync(req, ct)).ToCreatedAtAction(this, nameof(GetById), book => new { id = book.Id });

    [HttpPut("{id:int}")]
    [HasPermission(PermissionNames.BooksUpdate)]
    public async Task<IActionResult> Update(int id, UpdateBookRequest req, CancellationToken ct) =>
        (await booksService.UpdateAsync(id, req, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionNames.BooksDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        (await booksService.DeleteAsync(id, ct)).ToActionResult();
}
