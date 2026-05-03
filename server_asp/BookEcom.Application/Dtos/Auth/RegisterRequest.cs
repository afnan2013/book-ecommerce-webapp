using BookEcom.Domain.Auth;

namespace BookEcom.Application.Dtos.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserType UserType { get; set; }
}
