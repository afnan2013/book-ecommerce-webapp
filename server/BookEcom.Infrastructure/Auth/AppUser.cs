using BookEcom.Domain.Auth;
using Microsoft.AspNetCore.Identity;

namespace BookEcom.Infrastructure.Auth;

public class AppUser : IdentityUser<int>
{
    public UserType UserType { get; set; }
    public string FullName { get; set; } = "";
}
