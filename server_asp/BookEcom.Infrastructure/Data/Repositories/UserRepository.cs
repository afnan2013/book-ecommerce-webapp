using BookEcom.Domain.Abstractions;
using BookEcom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Infrastructure.Data.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<UserSnapshot?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserSnapshot
            {
                Id = u.Id,
                Email = u.Email ?? "",
                FullName = u.FullName,
                UserType = u.UserType,
                ConcurrencyStamp = u.ConcurrencyStamp ?? "",
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserSnapshot>> GetAllAsync(CancellationToken ct) =>
        await db.Users
            .AsNoTracking()
            .Select(u => new UserSnapshot
            {
                Id = u.Id,
                Email = u.Email ?? "",
                FullName = u.FullName,
                UserType = u.UserType,
                ConcurrencyStamp = u.ConcurrencyStamp ?? "",
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RoleSummary>> GetRolesForUserAsync(
        int userId, CancellationToken ct) =>
        await (from ur in db.UserRoles
               join r in db.Roles on ur.RoleId equals r.Id
               where ur.UserId == userId
               select new RoleSummary
               {
                   Id = r.Id,
                   Name = r.Name ?? "",
                   NormalizedName = r.NormalizedName ?? "",
                   ConcurrencyStamp = r.ConcurrencyStamp ?? "",
               })
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<RoleSummary>>> GetRolesForUsersAsync(
        IEnumerable<int> userIds, CancellationToken ct)
    {
        var idList = userIds.ToList();
        if (idList.Count == 0)
            return new Dictionary<int, IReadOnlyList<RoleSummary>>();

        var rows = await (from ur in db.UserRoles
                          join r in db.Roles on ur.RoleId equals r.Id
                          where idList.Contains(ur.UserId)
                          select new
                          {
                              ur.UserId,
                              Role = new RoleSummary
                              {
                                  Id = r.Id,
                                  Name = r.Name ?? "",
                                  NormalizedName = r.NormalizedName ?? "",
                                  ConcurrencyStamp = r.ConcurrencyStamp ?? "",
                              },
                          })
                       .AsNoTracking()
                       .ToListAsync(ct);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<RoleSummary>)g.Select(x => x.Role).ToList());
    }

    public async Task<IReadOnlyList<Permission>> GetDirectPermissionsForUserAsync(
        int userId, CancellationToken ct) =>
        await db.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => up.Permission)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<Permission>>> GetDirectPermissionsForUsersAsync(
        IEnumerable<int> userIds, CancellationToken ct)
    {
        var idList = userIds.ToList();
        if (idList.Count == 0)
            return new Dictionary<int, IReadOnlyList<Permission>>();

        var rows = await db.UserPermissions
            .AsNoTracking()
            .Where(up => idList.Contains(up.UserId))
            .Include(up => up.Permission)
            .ToListAsync(ct);

        return rows
            .GroupBy(up => up.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Permission>)g.Select(up => up.Permission).ToList());
    }

    public async Task ClearDirectPermissionsAsync(int userId, CancellationToken ct) =>
        await db.UserPermissions
            .Where(up => up.UserId == userId)
            .ExecuteDeleteAsync(ct);

    public void AddUserPermission(UserPermission userPermission) =>
        db.UserPermissions.Add(userPermission);

    public async Task<bool> UpdateConcurrencyStampAsync(
        int userId, string expectedStamp, string newStamp, CancellationToken ct)
    {
        var affected = await db.Users
            .Where(u => u.Id == userId && u.ConcurrencyStamp == expectedStamp)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.ConcurrencyStamp, newStamp),
                ct);
        return affected > 0;
    }

    public async Task<int> CountUsersInRoleAsync(int roleId, CancellationToken ct) =>
        await db.UserRoles.CountAsync(ur => ur.RoleId == roleId, ct);
}
