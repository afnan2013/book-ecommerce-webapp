using BookEcom.Domain.Auth;

namespace BookEcom.Domain.Abstractions;

/// <summary>
/// Pure-domain projection of an Identity user. Mirrors <c>RoleSummary</c>'s
/// purpose: keep <c>AppUser</c> (which inherits <c>IdentityUser&lt;int&gt;</c>
/// and lives in Infrastructure) out of <see cref="IUserRepository"/>'s read
/// surface so Domain stays framework-agnostic. Only the fields the Application
/// layer actually consumes are exposed.
/// </summary>
public class UserSnapshot
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserType UserType { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
}
