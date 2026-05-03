using System.ComponentModel.DataAnnotations;

namespace BookEcom.Application.Dtos.Roles;

public class SetRolePermissionsRequest
{
    public List<int> PermissionIds { get; set; } = [];

    /// <summary>
    /// The ConcurrencyStamp from the last GET. Used for optimistic concurrency —
    /// if someone else modified this role's permissions since you read it, the
    /// server will return 409 Conflict.
    /// </summary>
    [Required]
    public string ConcurrencyStamp { get; set; } = "";
}
