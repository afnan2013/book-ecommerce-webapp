using BookEcom.Api.Common.Results;

namespace BookEcom.Api.Entities;

/// <summary>
/// Book aggregate. Setters are private so the invariants enforced by
/// <see cref="Create"/> and <see cref="Update"/> cannot be bypassed by
/// application code. EF Core materializes instances by writing directly to
/// backing fields via reflection, so private setters are invisible to it —
/// the parameterless constructor is kept private purely for EF's use.
/// </summary>
public class Book
{
    public const int MaxTitleLength = 200;
    public const int MaxAuthorLength = 200;
    public const decimal MaxPrice = 100_000m;

    public int Id { get; private set; }
    public string Title { get; private set; } = "";
    public string Author { get; private set; } = "";
    public decimal Price { get; private set; }

    private Book() { }

    public static Result<Book> Create(string title, string author, decimal price)
    {
        var errors = Validate(title, author, price);
        if (errors.Count > 0)
            return Result<Book>.Validation("Invalid book data.", errors);

        return new Book
        {
            Title = title.Trim(),
            Author = author.Trim(),
            Price = price,
        };
    }

    public Result Update(string title, string author, decimal price)
    {
        var errors = Validate(title, author, price);
        if (errors.Count > 0)
            return Result.Validation("Invalid book data.", errors);

        Title = title.Trim();
        Author = author.Trim();
        Price = price;
        return Result.Success();
    }

    private static List<string> Validate(string title, string author, decimal price)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
            errors.Add("Title is required.");
        else if (title.Trim().Length > MaxTitleLength)
            errors.Add($"Title must be {MaxTitleLength} characters or fewer.");

        if (string.IsNullOrWhiteSpace(author))
            errors.Add("Author is required.");
        else if (author.Trim().Length > MaxAuthorLength)
            errors.Add($"Author must be {MaxAuthorLength} characters or fewer.");

        if (price <= 0m)
            errors.Add("Price must be greater than zero.");
        else if (price > MaxPrice)
            errors.Add($"Price must not exceed {MaxPrice}.");

        return errors;
    }
}
