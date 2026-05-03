using BookEcom.Domain.Abstractions;
using BookEcom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Infrastructure.Data.Repositories;

public class RoleRepository(AppDbContext db) : IRoleRepository
{
    public async Task<IReadOnlyList<RoleSummary>> GetAllAsync(CancellationToken ct) =>
        await db.Roles
            .AsNoTracking()
            .Select(r => new RoleSummary
            {
                Id = r.Id,
                Name = r.Name ?? "",
                NormalizedName = r.NormalizedName ?? "",
                ConcurrencyStamp = r.ConcurrencyStamp ?? "",
            })
            .ToListAsync(ct);

    public async Task<RoleSummary?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.Roles
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoleSummary
            {
                Id = r.Id,
                Name = r.Name ?? "",
                NormalizedName = r.NormalizedName ?? "",
                ConcurrencyStamp = r.ConcurrencyStamp ?? "",
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<RoleSummary>> GetByIdsAsync(
        IEnumerable<int> ids, CancellationToken ct)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];

        return await db.Roles
            .AsNoTracking()
            .Where(r => idList.Contains(r.Id))
            .Select(r => new RoleSummary
            {
                Id = r.Id,
                Name = r.Name ?? "",
                NormalizedName = r.NormalizedName ?? "",
                ConcurrencyStamp = r.ConcurrencyStamp ?? "",
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsForRoleAsync(
        int roleId, CancellationToken ct) =>
        await db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<Permission>>> GetPermissionsForRolesAsync(
        IEnumerable<int> roleIds, CancellationToken ct)
    {
        var idList = roleIds.ToList();
        if (idList.Count == 0)
            return new Dictionary<int, IReadOnlyList<Permission>>();

        var rows = await db.RolePermissions
            .AsNoTracking()
            .Where(rp => idList.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .ToListAsync(ct);

        return rows
            .GroupBy(rp => rp.RoleId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Permission>)g.Select(rp => rp.Permission).ToList());
    }

    public async Task ClearPermissionsAsync(int roleId, CancellationToken ct) =>
        await db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ExecuteDeleteAsync(ct);

    public void AddRolePermission(RolePermission rolePermission) =>
        db.RolePermissions.Add(rolePermission);

    public async Task<bool> UpdateConcurrencyStampAsync(
        int roleId, string expectedStamp, string newStamp, CancellationToken ct)
    {
        var affected = await db.Roles
            .Where(r => r.Id == roleId && r.ConcurrencyStamp == expectedStamp)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.ConcurrencyStamp, newStamp),
                ct);
        return affected > 0;
    }
}
