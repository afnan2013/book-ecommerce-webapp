using BookEcom.Api.Auth;
using BookEcom.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Data.Seed;

/// <summary>
/// Runs once at app startup. Idempotent — safe to run on every boot.
///
/// Responsibilities, in order:
///   1. Sync the permissions catalog with PermissionNames in code.
///   2. Ensure the SuperAdmin role exists and always has every permission.
///   3. Bootstrap Moderator / SupportAdmin default roles on a fresh DB only.
///   4. Create the default admin user and make sure they're in SuperAdmin.
/// </summary>
public class IdentitySeeder(IServiceProvider services, ILogger<IdentitySeeder> logger) : IHostedService
{
    private const string DefaultAdminEmail = "admin@bookecom.local";
    private const string DefaultAdminPassword = "Admin!ChangeMe1";

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            // Detect fresh-DB state BEFORE we create SuperAdmin, so bootstrap
            // decisions later are based on the original state of the database.
            var isFreshDb = !await db.Roles.AnyAsync(ct);

            await SyncPermissionsCatalogAsync(db, ct);
            await EnsureSuperAdminRoleAsync(db, roleManager, ct);
            if (isFreshDb) await BootstrapDefaultRolesAsync(db, roleManager, ct);
            await EnsureDefaultAdminAsync(userManager, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "IdentitySeeder failed. Migrations may not have been applied yet — " +
                "run `dotnet ef database update` and restart.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    // ──────────────────────────────────────────────────────────────────────
    // 1. Sync the permissions catalog with the code constants.
    //    Adds any PermissionNames.Descriptions entry missing from the DB.
    //    Removes any DB row not in code, along with its role_permissions and
    //    user_permissions links (those grants point at a permission no code
    //    checks for anymore — they're effectively dead, not worth preserving).
    // ──────────────────────────────────────────────────────────────────────
    private async Task SyncPermissionsCatalogAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Name, ct);

        // Upsert code-defined permissions.
        foreach (var (name, description) in PermissionNames.Descriptions)
        {
            if (existing.TryGetValue(name, out var row))
            {
                if (row.Description != description)
                {
                    row.Description = description;
                    logger.LogInformation("Updated description for permission {Name}", name);
                }
            }
            else
            {
                db.Permissions.Add(new Permission { Name = name, Description = description });
                logger.LogInformation("Added permission {Name}", name);
            }
        }

        // Delete orphan permissions (in DB but not in code) and their dependents.
        var codeNames = PermissionNames.All.ToHashSet();
        var orphans = existing.Values.Where(r => !codeNames.Contains(r.Name)).ToList();

        foreach (var orphan in orphans)
        {
            var roleCount = await db.RolePermissions
                .Where(rp => rp.PermissionId == orphan.Id)
                .ExecuteDeleteAsync(ct);

            var userCount = await db.UserPermissions
                .Where(up => up.PermissionId == orphan.Id)
                .ExecuteDeleteAsync(ct);

            db.Permissions.Remove(orphan);

            logger.LogWarning(
                "Removed orphan permission {Name} (no longer in code). " +
                "Unlinked from {RoleCount} role(s) and {UserCount} user(s).",
                orphan.Name, roleCount, userCount);
        }

        await db.SaveChangesAsync(ct);
    }

    // ──────────────────────────────────────────────────────────────────────
    // 2. Ensure SuperAdmin exists and has every permission in the catalog.
    //    Reconciles on every boot, so new permissions auto-flow to SuperAdmin.
    // ──────────────────────────────────────────────────────────────────────
    private async Task EnsureSuperAdminRoleAsync(
        AppDbContext db,
        RoleManager<IdentityRole<int>> roleManager,
        CancellationToken ct)
    {
        var role = await roleManager.FindByNameAsync(RoleNames.SuperAdmin);
        if (role is null)
        {
            role = new IdentityRole<int> { Name = RoleNames.SuperAdmin };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create SuperAdmin role: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Created {Role} role", RoleNames.SuperAdmin);
        }

        var allPermissions = await db.Permissions.ToListAsync(ct);
        var existingLinks = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync(ct);

        foreach (var perm in allPermissions.Where(p => !existingLinks.Contains(p.Id)))
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = perm.Id,
            });
            logger.LogInformation("Granted {Permission} to SuperAdmin", perm.Name);
        }

        await db.SaveChangesAsync(ct);
    }

    // ──────────────────────────────────────────────────────────────────────
    // 3. Bootstrap Moderator / SupportAdmin on a fresh DB only.
    //    If an admin deleted one of these later, we don't resurrect it —
    //    that would undo their decision.
    // ──────────────────────────────────────────────────────────────────────
    private async Task BootstrapDefaultRolesAsync(
        AppDbContext db,
        RoleManager<IdentityRole<int>> roleManager,
        CancellationToken ct)
    {
        var defaults = new Dictionary<string, string[]>
        {
            // Moderator: full book management + non-destructive user management.
            // No users.delete, no users.create — moderators moderate, they
            // don't onboard or offboard people.
            ["Moderator"] = new[]
            {
                PermissionNames.BooksRead,
                PermissionNames.BooksCreate,
                PermissionNames.BooksUpdate,
                PermissionNames.BooksDelete,
                PermissionNames.UsersRead,
                PermissionNames.UsersUpdate,
            },

            // SupportAdmin: read-only on users. Meant for customer-support
            // staff who look up account details to help but don't mutate.
            ["SupportAdmin"] = new[]
            {
                PermissionNames.UsersRead,
            },
        };

        foreach (var (roleName, permNames) in defaults)
        {
            var role = new IdentityRole<int> { Name = roleName };
            var created = await roleManager.CreateAsync(role);
            if (!created.Succeeded)
            {
                logger.LogError("Failed to bootstrap {Role}: {Errors}",
                    roleName, string.Join("; ", created.Errors.Select(e => e.Description)));
                continue;
            }
            logger.LogInformation("Bootstrapped {Role}", roleName);

            foreach (var permName in permNames)
            {
                var perm = await db.Permissions.FirstOrDefaultAsync(p => p.Name == permName, ct);
                if (perm is null) continue;

                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // ──────────────────────────────────────────────────────────────────────
    // 4. Ensure the default admin user exists and is in SuperAdmin.
    // ──────────────────────────────────────────────────────────────────────
    private async Task EnsureDefaultAdminAsync(UserManager<AppUser> userManager, CancellationToken ct)
    {
        var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);

        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                FullName = "Default SuperAdmin",
                UserType = UserType.Admin,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(admin, DefaultAdminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create default admin: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogWarning(
                "Seeded default admin {Email} with placeholder password. " +
                "CHANGE IT IMMEDIATELY in any non-throwaway environment.",
                DefaultAdminEmail);
        }

        // Idempotent — only adds the link if it's not already there.
        if (!await userManager.IsInRoleAsync(admin, RoleNames.SuperAdmin))
        {
            await userManager.AddToRoleAsync(admin, RoleNames.SuperAdmin);
            logger.LogInformation("Assigned {Role} to {Email}", RoleNames.SuperAdmin, admin.Email);
        }
    }
}
