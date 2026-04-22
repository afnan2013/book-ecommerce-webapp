using BookEcom.Api.Auth;
using BookEcom.Api.Data;
using BookEcom.Api.Dtos.Auth;
using BookEcom.Api.Dtos.Permissions;
using BookEcom.Api.Dtos.Users;
using BookEcom.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    AppDbContext db,
    ILogger<UsersController> logger) : ControllerBase
{
    // GET /api/users/me
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            UserType = user.UserType,
        });
    }

    // POST /api/users
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest req, CancellationToken ct)
    {
        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName,
            UserType = req.UserType,
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation(
            "POST /api/users — admin created {UserType} {Email}",
            req.UserType, req.Email);

        var response = await BuildUserResponse(user, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        // Protect: admin cannot delete their own account (would invalidate their session).
        var currentUserIdStr = userManager.GetUserId(User);
        if (int.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id)
        {
            return BadRequest(new { error = "You cannot delete your own account." });
        }

        // Protect: cannot delete the last SuperAdmin.
        var userRoles = await userManager.GetRolesAsync(user);
        if (userRoles.Contains(RoleNames.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            var superAdminRole = await roleManager.FindByNameAsync(RoleNames.SuperAdmin);
            if (superAdminRole is not null)
            {
                var superAdminCount = await db.UserRoles
                    .CountAsync(ur => ur.RoleId == superAdminRole.Id, ct);
                if (superAdminCount <= 1)
                {
                    return BadRequest(new
                    {
                        error = "Cannot delete the last SuperAdmin user."
                    });
                }
            }
        }

        // ── Explicit transaction ───────────────────────────────────────────
        // Two writes must succeed together: our custom UserPermissions table
        // (EF cascade SHOULD cover it, but being explicit documents intent)
        // and the Identity user row (which cascades UserRoles via FK).
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.UserPermissions.Where(up => up.UserId == id).ExecuteDeleteAsync(ct);

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        logger.LogInformation("DELETE /api/users/{Id} — deleted {Email}", id, user.Email);
        return NoContent();
    }

    // GET /api/users
    [HttpGet]
    public async Task<IEnumerable<UserResponse>> GetAll(CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().ToListAsync(ct);

        // Load all user-role and user-permission mappings in bulk to avoid N+1.
        var userRoles = await db.UserRoles.AsNoTracking().ToListAsync(ct);
        var roles = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, ct);
        var userPermissions = await db.UserPermissions
            .AsNoTracking()
            .Include(up => up.Permission)
            .ToListAsync(ct);

        var result = users.Select(u => new UserResponse
        {
            Id = u.Id,
            Email = u.Email ?? "",
            FullName = u.FullName,
            UserType = u.UserType,
            ConcurrencyStamp = u.ConcurrencyStamp ?? "",
            Roles = userRoles
                .Where(ur => ur.UserId == u.Id)
                .Select(ur => roles.TryGetValue(ur.RoleId, out var role)
                    ? new UserRoleDto { Id = role.Id, Name = role.Name! }
                    : null)
                .Where(r => r is not null)
                .ToList()!,
            DirectPermissions = userPermissions
                .Where(up => up.UserId == u.Id)
                .Select(up => new PermissionDto
                {
                    Id = up.Permission.Id,
                    Name = up.Permission.Name,
                    Description = up.Permission.Description,
                })
                .ToList(),
        });

        logger.LogInformation("GET /api/users — returning {Count} users", users.Count);
        return result;
    }

    // GET /api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        var roleIds = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == id)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => new UserRoleDto { Id = r.Id, Name = r.Name! })
            .ToListAsync(ct);

        var directPermissions = await db.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == id)
            .Include(up => up.Permission)
            .Select(up => new PermissionDto
            {
                Id = up.Permission.Id,
                Name = up.Permission.Name,
                Description = up.Permission.Description,
            })
            .ToListAsync(ct);

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            UserType = user.UserType,
            ConcurrencyStamp = user.ConcurrencyStamp ?? "",
            Roles = roles,
            DirectPermissions = directPermissions,
        });
    }

    // PUT /api/users/{id}/roles
    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult<UserResponse>> SetRoles(
        int id, SetUserRolesRequest req, CancellationToken ct)
    {
        // Track the user (not AsNoTracking) so EF can detect concurrency conflicts.
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        // ── Optimistic concurrency: early check ────────────────────────────
        if (user.ConcurrencyStamp != req.ConcurrencyStamp)
        {
            return Conflict(new { error = "This user was modified by someone else. Please refresh and try again." });
        }

        // Validate all requested role IDs exist.
        var requestedRoles = await db.Roles
            .Where(r => req.RoleIds.Contains(r.Id))
            .ToListAsync(ct);

        var invalidIds = req.RoleIds.Except(requestedRoles.Select(r => r.Id)).ToList();
        if (invalidIds.Count > 0)
        {
            return BadRequest(new { error = $"Invalid role IDs: {string.Join(", ", invalidIds)}" });
        }

        // Protect: cannot remove SuperAdmin role from the last SuperAdmin.
        var currentRoles = await userManager.GetRolesAsync(user);
        var isSuperAdmin = currentRoles.Contains(RoleNames.SuperAdmin, StringComparer.OrdinalIgnoreCase);
        var keepsSuperAdmin = requestedRoles.Any(r =>
            string.Equals(r.NormalizedName, RoleNames.SuperAdmin.ToUpperInvariant(), StringComparison.Ordinal));

        if (isSuperAdmin && !keepsSuperAdmin)
        {
            var superAdminRole = await roleManager.FindByNameAsync(RoleNames.SuperAdmin);
            if (superAdminRole is not null)
            {
                var superAdminCount = await db.UserRoles
                    .CountAsync(ur => ur.RoleId == superAdminRole.Id, ct);

                if (superAdminCount <= 1)
                {
                    return BadRequest(new
                    {
                        error = "Cannot remove SuperAdmin role from the last SuperAdmin user."
                    });
                }
            }
        }

        // ── Explicit transaction ───────────────────────────────────────────
        // UserManager.RemoveFromRolesAsync and AddToRolesAsync each call
        // SaveChangesAsync internally. Without a transaction, if Remove
        // succeeds but Add fails, the user is left with zero roles.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                return BadRequest(new { errors = removeResult.Errors.Select(e => e.Description) });
            }

            var newRoleNames = requestedRoles.Select(r => r.Name!).ToList();
            if (newRoleNames.Count > 0)
            {
                var addResult = await userManager.AddToRolesAsync(user, newRoleNames);
                if (!addResult.Succeeded)
                {
                    await transaction.RollbackAsync(ct);
                    return BadRequest(new { errors = addResult.Errors.Select(e => e.Description) });
                }
            }

            // Bump SecurityStamp so the refresh-token flow can invalidate this
            // user's session when their roles change. Currently inert — JWT bearer
            // in Program.cs doesn't validate SecurityStamp. Phase TBD: refresh tokens.
            // Side effect: Identity's UpdateAsync also bumps ConcurrencyStamp, which
            // is what actually protects the next concurrent request.
            await userManager.UpdateSecurityStampAsync(user);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Conflict(new { error = "This user was modified by someone else. Please refresh and try again." });
        }

        logger.LogInformation(
            "PUT /api/users/{Id}/roles — set {Count} roles on {Email}",
            id, requestedRoles.Count, user.Email);

        return Ok(await BuildUserResponse(user, ct));
    }

    // PUT /api/users/{id}/permissions
    [HttpPut("{id:int}/permissions")]
    public async Task<ActionResult<UserResponse>> SetPermissions(
        int id, SetUserPermissionsRequest req, CancellationToken ct)
    {
        // Track the user so EF can detect concurrency conflicts.
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        // ── Optimistic concurrency: early check ────────────────────────────
        if (user.ConcurrencyStamp != req.ConcurrencyStamp)
        {
            return Conflict(new { error = "This user was modified by someone else. Please refresh and try again." });
        }

        // Validate all requested permission IDs exist.
        var validPermissions = await db.Permissions
            .Where(p => req.PermissionIds.Contains(p.Id))
            .ToListAsync(ct);

        var invalidIds = req.PermissionIds.Except(validPermissions.Select(p => p.Id)).ToList();
        if (invalidIds.Count > 0)
        {
            return BadRequest(new { error = $"Invalid permission IDs: {string.Join(", ", invalidIds)}" });
        }

        // ── Explicit transaction ───────────────────────────────────────────
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.UserPermissions.Where(up => up.UserId == id).ExecuteDeleteAsync(ct);

            foreach (var perm in validPermissions)
            {
                db.UserPermissions.Add(new UserPermission
                {
                    UserId = id,
                    PermissionId = perm.Id,
                });
            }

            // Bump ConcurrencyStamp so the next concurrent request gets a 409.
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Conflict(new { error = "This user was modified by someone else. Please refresh and try again." });
        }

        logger.LogInformation(
            "PUT /api/users/{Id}/permissions — set {Count} direct permissions on {Email}",
            id, validPermissions.Count, user.Email);

        return Ok(await BuildUserResponse(user, ct));
    }

    /// <summary>
    /// Builds a full UserResponse with roles and direct permissions.
    /// Reused by SetRoles and SetPermissions to return the updated state.
    /// </summary>
    private async Task<UserResponse> BuildUserResponse(AppUser user, CancellationToken ct)
    {
        var roleNames = await userManager.GetRolesAsync(user);
        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => new UserRoleDto { Id = r.Id, Name = r.Name! })
            .ToListAsync(ct);

        var directPermissions = await db.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == user.Id)
            .Include(up => up.Permission)
            .Select(up => new PermissionDto
            {
                Id = up.Permission.Id,
                Name = up.Permission.Name,
                Description = up.Permission.Description,
            })
            .ToListAsync(ct);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            UserType = user.UserType,
            ConcurrencyStamp = user.ConcurrencyStamp ?? "",
            Roles = roles,
            DirectPermissions = directPermissions,
        };
    }
}
