using BookEcom.Domain.Auth;
using Microsoft.AspNetCore.Identity;

namespace BookEcom.Application.Auth;

/// <summary>
/// Identity user for this app. Inherits <see cref="IdentityUser{TKey}"/> from
/// ASP.NET Core Identity, so it can never be a "pure" Domain entity — Identity
/// types come with framework dependencies (password hashing, lockout, security
/// stamp). It lives in Application rather than Infrastructure because
/// Application is where the auth use cases live; Infrastructure now references
/// Application instead of the other way around (see project graph in CLAUDE.md
/// — phase 5b flipped that arrow).
/// </summary>
public class AppUser : IdentityUser<int>
{
    public UserType UserType { get; set; }
    public string FullName { get; set; } = "";
}
