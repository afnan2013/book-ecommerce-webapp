using BookEcom.Application.Users.Policies;
using BookEcom.Application.Dtos.Auth;
using BookEcom.Application.Dtos.Users;
using BookEcom.Domain.Common.Results;
using BookEcom.Domain.Entities;
using BookEcom.Infrastructure.Auth;
using BookEcom.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Application.Users;

public class UserManagementService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    LastSuperAdminPolicy lastSuperAdminPolicy,
    UserResponseProjector projector,
    ILogger<UserManagementService> logger) : IUserManagementService
{
    public async Task<Result<UserDto>> GetMeAsync(int callerId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == callerId, ct);
        if (user is null) return Result<UserDto>.Unauthorized("User not found.");

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            UserType = user.UserType,
        };
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().ToListAsync(ct);
        logger.LogInformation("Users.GetAll — returning {Count} users", users.Count);
        return await projector.ProjectManyAsync(users, ct);
    }

    public async Task<Result<UserResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Result<UserResponse>.NotFound($"User {id} not found.");
        return await projector.ProjectOneAsync(user, ct);
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest req, CancellationToken ct)
    {
        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName,
            UserType = req.UserType,
        };

        var created = await userManager.CreateAsync(user, req.Password);
        if (!created.Succeeded)
        {
            return Result<UserResponse>.Validation(
                "Could not create user.",
                created.Errors.Select(e => e.Description).ToList());
        }

        logger.LogInformation(
            "Users.Create — admin created {UserType} {Email}", req.UserType, req.Email);
        return await projector.ProjectOneAsync(user, ct);
    }

    public async Task<Result> DeleteAsync(int id, int callerId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.NotFound($"User {id} not found.");

        if (callerId == id)
            return Result.Validation("You cannot delete your own account.");

        var policyCheck = await lastSuperAdminPolicy.CanDeleteAsync(user, ct);
        if (policyCheck.IsFailure) return policyCheck;

        // Explicit transaction: our UserPermissions wipe + Identity user
        // delete (which cascades AspNetUserRoles) must succeed together.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.UserPermissions.Where(up => up.UserId == id).ExecuteDeleteAsync(ct);

            var deleted = await userManager.DeleteAsync(user);
            if (!deleted.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                return Result.Validation(
                    "Could not delete user.",
                    deleted.Errors.Select(e => e.Description).ToList());
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        logger.LogInformation("Users.Delete — deleted {Id} ({Email})", id, user.Email);
        return Result.Success();
    }

    public async Task<Result<UserResponse>> SetRolesAsync(
        int id, SetUserRolesRequest req, CancellationToken ct)
    {
        // Ordering is load-bearing: fetch → concurrency → validate → policy
        // → remove → add → bump stamp → commit. Do not reorder without tests.
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result<UserResponse>.NotFound($"User {id} not found.");

        if (user.ConcurrencyStamp != req.ConcurrencyStamp)
        {
            return Result<UserResponse>.Conflict(
                "This user was modified by someone else. Please refresh and try again.");
        }

        var requestedRoles = await db.Roles
            .Where(r => req.RoleIds.Contains(r.Id))
            .ToListAsync(ct);

        var invalidIds = req.RoleIds.Except(requestedRoles.Select(r => r.Id)).ToList();
        if (invalidIds.Count > 0)
        {
            return Result<UserResponse>.Validation(
                $"Invalid role IDs: {string.Join(", ", invalidIds)}");
        }

        var policyCheck = await lastSuperAdminPolicy.CanDemoteAsync(user, requestedRoles, ct);
        if (policyCheck.IsFailure) return policyCheck.Error!;

        var currentRoles = await userManager.GetRolesAsync(user);

        // Transaction: remove+add must commit together, else a failed Add
        // after a successful Remove leaves the user role-less.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                return Result<UserResponse>.Validation(
                    "Could not update roles.",
                    removeResult.Errors.Select(e => e.Description).ToList());
            }

            var newRoleNames = requestedRoles.Select(r => r.Name!).ToList();
            if (newRoleNames.Count > 0)
            {
                var addResult = await userManager.AddToRolesAsync(user, newRoleNames);
                if (!addResult.Succeeded)
                {
                    await transaction.RollbackAsync(ct);
                    return Result<UserResponse>.Validation(
                        "Could not update roles.",
                        addResult.Errors.Select(e => e.Description).ToList());
                }
            }

            // Bump SecurityStamp so a future refresh-token flow can invalidate
            // this user's sessions on role change. Currently inert — JWT bearer
            // does not validate SecurityStamp. Side effect: Identity.UpdateAsync
            // also bumps ConcurrencyStamp, which is what protects the next write.
            await userManager.UpdateSecurityStampAsync(user);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<UserResponse>.Conflict(
                "This user was modified by someone else. Please refresh and try again.");
        }

        logger.LogInformation(
            "Users.SetRoles — set {Count} roles on {Email}",
            requestedRoles.Count, user.Email);
        return await projector.ProjectOneAsync(user, ct);
    }

    public async Task<Result<UserResponse>> SetPermissionsAsync(
        int id, SetUserPermissionsRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result<UserResponse>.NotFound($"User {id} not found.");

        if (user.ConcurrencyStamp != req.ConcurrencyStamp)
        {
            return Result<UserResponse>.Conflict(
                "This user was modified by someone else. Please refresh and try again.");
        }

        var validPermissions = await db.Permissions
            .Where(p => req.PermissionIds.Contains(p.Id))
            .ToListAsync(ct);

        var invalidIds = req.PermissionIds.Except(validPermissions.Select(p => p.Id)).ToList();
        if (invalidIds.Count > 0)
        {
            return Result<UserResponse>.Validation(
                $"Invalid permission IDs: {string.Join(", ", invalidIds)}");
        }

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

            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<UserResponse>.Conflict(
                "This user was modified by someone else. Please refresh and try again.");
        }

        logger.LogInformation(
            "Users.SetPermissions — set {Count} direct permissions on {Email}",
            validPermissions.Count, user.Email);
        return await projector.ProjectOneAsync(user, ct);
    }
}
