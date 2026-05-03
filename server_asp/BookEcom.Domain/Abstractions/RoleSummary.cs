namespace BookEcom.Domain.Abstractions;

/// <summary>
/// Pure-domain projection of an Identity role. Exists so <see cref="IRoleRepository"/>
/// can return role data without leaking <c>IdentityRole&lt;int&gt;</c> (and its
/// <c>Microsoft.AspNetCore.Identity</c> package dependency) into Domain.
/// Lives next to the contract it supports rather than in <c>Entities/</c>
/// because it is a read model, not an aggregate with identity semantics.
/// </summary>
public class RoleSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string ConcurrencyStamp { get; set; } = "";
}
