using System.ComponentModel.DataAnnotations;

namespace BookEcom.Api.Dtos.Books;

public class UpdateBookRequest
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
