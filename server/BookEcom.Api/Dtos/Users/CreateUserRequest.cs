using System.ComponentModel.DataAnnotations;
using BookEcom.Domain.Auth;

namespace BookEcom.Api.Dtos.Users;

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = "";

    [Required]
    [MinLength(1)]
    public string FullName { get; set; } = "";

    [Required]
    public UserType UserType { get; set; }
}
