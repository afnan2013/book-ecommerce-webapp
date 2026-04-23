using System.ComponentModel.DataAnnotations;

namespace BookEcom.Application.Dtos.Books;

public class CreateBookRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = "";

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Author { get; set; } = "";

    [Range(0.01, 100_000)]
    public decimal Price { get; set; }
}
