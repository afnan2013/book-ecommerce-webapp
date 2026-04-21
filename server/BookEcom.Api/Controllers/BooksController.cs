using BookEcom.Api.Data;
using BookEcom.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BooksController(AppDbContext db, ILogger<BooksController> logger) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<BooksController> _logger = logger;

    // GET /api/books
    [HttpGet]
    public async Task<IEnumerable<Book>> GetAll(CancellationToken ct)
    {
        var books = await _db.Books.AsNoTracking().ToListAsync(ct);
        _logger.LogInformation("GET /api/books — returning {Count} books", books.Count);
        return books;
    }

    // GET /api/books/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Book>> GetById(int id, CancellationToken ct)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        return book is null ? NotFound() : Ok(book);
    }

    // POST /api/books
    [HttpPost]
    public async Task<ActionResult<Book>> Create(Book book, CancellationToken ct)
    {
        _db.Books.Add(book);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("POST /api/books — created book {Id}", book.Id);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    // PUT /api/books/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Book updated, CancellationToken ct)
    {
        var existing = await _db.Books.FindAsync([id], ct);
        if (existing is null) return NotFound();

        existing.Title  = updated.Title;
        existing.Author = updated.Author;
        existing.Price  = updated.Price;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/books/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var book = await _db.Books.FindAsync([id], ct);
        if (book is null) return NotFound();

        _db.Books.Remove(book);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
