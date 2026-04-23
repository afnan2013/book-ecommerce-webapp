using BookEcom.Domain.Auth;

namespace BookEcom.Application.Dtos.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserType UserType { get; set; }
}
