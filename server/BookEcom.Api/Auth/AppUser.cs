using Microsoft.AspNetCore.Identity;

namespace BookEcom.Api.Auth;

public class AppUser : IdentityUser<int>
{
    public UserType UserType { get; set; }
    public string FullName { get; set; } = "";
}
