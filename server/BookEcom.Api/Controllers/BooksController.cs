using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(ILogger<BooksController> logger) : ControllerBase
{
    private readonly ILogger<BooksController> _logger = logger;
    private static readonly List<Book> _books =
    [
        new Book { Id = 1, Title = "Clean Code",                            Author = "Robert C. Martin",            Price = 29.99m },
        new Book { Id = 2, Title = "The Pragmatic Programmer",              Author = "Andrew Hunt & David Thomas",  Price = 34.50m },
        new Book { Id = 3, Title = "Designing Data-Intensive Applications", Author = "Martin Kleppmann",            Price = 42.00m },
    ];

    // GET /api/books
    [HttpGet]
    public IEnumerable<Book> GetAll()
    {
        _logger.LogInformation("GET /api/books — returning {Count} books", _books.Count);
        return _books;
    }

    // GET /api/books/{id}
    [HttpGet("{id:int}")]
    public ActionResult<Book> GetById(int id)
    {
        var book = _books.FirstOrDefault(b => b.Id == id);
        return book is null ? NotFound() : Ok(book);
    }

    // POST /api/books
    [HttpPost]
    public ActionResult<Book> Create(Book book)
    {
        book.Id = _books.Count == 0 ? 1 : _books.Max(b => b.Id) + 1;
        _books.Add(book);
        _logger.LogInformation("POST /api/books — created book {Id}", book.Id);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    // PUT /api/books/{id}
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, Book updated)
    {
        var existing = _books.FirstOrDefault(b => b.Id == id);
        if (existing is null) return NotFound();

        existing.Title  = updated.Title;
        existing.Author = updated.Author;
        existing.Price  = updated.Price;

        return NoContent();
    }

    // DELETE /api/books/{id}
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var book = _books.FirstOrDefault(b => b.Id == id);
        if (book is null) return NotFound();

        _books.Remove(book);
        return NoContent();
    }
}
