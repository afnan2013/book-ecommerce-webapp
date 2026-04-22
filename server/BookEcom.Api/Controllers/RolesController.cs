using BookEcom.Api.Auth;
using BookEcom.Api.Data;
using BookEcom.Api.Dtos.Permissions;
using BookEcom.Api.Dtos.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RolesController(
    RoleManager<IdentityRole<int>> roleManager,
    AppDbContext db,
    ILogger<RolesController> logger) : ControllerBase
{
    // GET /api/roles
    [HttpGet]
    public async Task<IEnumerable<RoleResponse>> GetAll(CancellationToken ct)
    {
        // Left-join roles → role_permissions → permissions in a single query.
        // GroupBy in-memory because EF translates this more predictably than
        // trying to group on the DB side with navigation-less joins.
        var rolePermissions = await db.Roles
            .AsNoTracking()
            .GroupJoin(
                db.RolePermissions.Include(rp => rp.Permission),
                role => role.Id,
                rp => rp.RoleId,
                (role, perms) => new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name!,
                    ConcurrencyStamp = role.ConcurrencyStamp!,
                    Permissions = perms.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                    }).ToList(),
                })
            .ToListAsync(ct);

        logger.LogInformation("GET /api/roles — returning {Count} roles", rolePermissions.Count);
        return rolePermissions;
    }

    // GET /api/roles/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleResponse>> GetById(int id, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null) return NotFound();

        var permissions = await db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == id)
            .Include(rp => rp.Permission)
            .Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
            })
            .ToListAsync(ct);

        return Ok(new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            ConcurrencyStamp = role.ConcurrencyStamp!,
            Permissions = permissions,
        });
    }

    // POST /api/roles
    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create(CreateRoleRequest req, CancellationToken ct)
    {
        var role = new IdentityRole<int> { Name = req.Name };
        var result = await roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("POST /api/roles — created role {Id} ({Name})", role.Id, role.Name);

        var response = new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            ConcurrencyStamp = role.ConcurrencyStamp!,
            Permissions = [],
        };

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, response);
    }

    // PUT /api/roles/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRoleRequest req, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null) return NotFound();

        if (role.NormalizedName == RoleNames.SuperAdmin.ToUpperInvariant())
        {
            return BadRequest(new { error = "SuperAdmin role cannot be modified." });
        }

        role.Name = req.Name;
        var result = await roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("PUT /api/roles/{Id} — renamed to {Name}", id, req.Name);
        return NoContent();
    }

    // DELETE /api/roles/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null) return NotFound();

        if (role.NormalizedName == RoleNames.SuperAdmin.ToUpperInvariant())
        {
            return BadRequest(new { error = "SuperAdmin role cannot be deleted." });
        }

        // Remove permission links first, then the role itself.
        await db.RolePermissions.Where(rp => rp.RoleId == id).ExecuteDeleteAsync(ct);

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("DELETE /api/roles/{Id} — deleted {Name}", id, role.Name);
        return NoContent();
    }

    // PUT /api/roles/{id}/permissions
    [HttpPut("{id:int}/permissions")]
    public async Task<ActionResult<RoleResponse>> SetPermissions(
        int id, SetRolePermissionsRequest req, CancellationToken ct)
    {
        // Track the role (not AsNoTracking) so EF can detect concurrency conflicts.
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null) return NotFound();

        if (role.NormalizedName == RoleNames.SuperAdmin.ToUpperInvariant())
        {
            return BadRequest(new { error = "SuperAdmin permissions are managed automatically and cannot be modified." });
        }

        // ── Optimistic concurrency check ───────────────────────────────────
        // The client sends the ConcurrencyStamp it received from GET.
        // If another request modified this role since then, the stamps won't
        // match and RoleManager.UpdateAsync will throw DbUpdateConcurrencyException.
        if (role.ConcurrencyStamp != req.ConcurrencyStamp)
        {
            return Conflict(new { error = "This role was modified by someone else. Please refresh and try again." });
        }

        // Validate that every requested permission ID actually exists.
        var validPermissions = await db.Permissions
            .Where(p => req.PermissionIds.Contains(p.Id))
            .ToListAsync(ct);

        var invalidIds = req.PermissionIds.Except(validPermissions.Select(p => p.Id)).ToList();
        if (invalidIds.Count > 0)
        {
            return BadRequest(new { error = $"Invalid permission IDs: {string.Join(", ", invalidIds)}" });
        }

        // ── Explicit transaction ───────────────────────────────────────────
        // ExecuteDeleteAsync runs immediately outside the change tracker, so
        // we wrap everything in a transaction. If the Add+SaveChanges fails,
        // the delete is rolled back too — no half-deleted state.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.RolePermissions.Where(rp => rp.RoleId == id).ExecuteDeleteAsync(ct);

            foreach (var perm in validPermissions)
            {
                db.RolePermissions.Add(new Entities.RolePermission
                {
                    RoleId = id,
                    PermissionId = perm.Id,
                });
            }

            // Bump the role's ConcurrencyStamp so the next concurrent request
            // will see a mismatch and get a 409.
            role.ConcurrencyStamp = Guid.NewGuid().ToString();
            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Conflict(new { error = "This role was modified by someone else. Please refresh and try again." });
        }

        logger.LogInformation(
            "PUT /api/roles/{Id}/permissions — set {Count} permissions on {Name}",
            id, validPermissions.Count, role.Name);

        return Ok(new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            ConcurrencyStamp = role.ConcurrencyStamp,
            Permissions = validPermissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
            }).ToList(),
        });
    }
}
