using BookEcom.Api.Data;
using BookEcom.Api.Dtos.Permissions;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Application.Permissions;

public class PermissionService(AppDbContext db, ILogger<PermissionService> logger) : IPermissionService
{
    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
            })
            .ToListAsync(ct);

        logger.LogInformation("Permissions.GetAll — returning {Count} permissions", permissions.Count);
        return permissions;
    }
}
