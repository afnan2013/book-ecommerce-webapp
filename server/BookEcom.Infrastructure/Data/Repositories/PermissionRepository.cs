using BookEcom.Domain.Abstractions;
using BookEcom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Infrastructure.Data.Repositories;

public class PermissionRepository(AppDbContext db) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct) =>
        await db.Permissions.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(
        IEnumerable<int> ids, CancellationToken ct)
    {
        // Materialize once — if the caller streams the enumerable into EF's
        // Contains filter without buffering, some providers re-enumerate it.
        var idList = ids.ToList();
        return await db.Permissions
            .AsNoTracking()
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(ct);
    }
}
