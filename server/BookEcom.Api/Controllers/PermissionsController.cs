using BookEcom.Api.Data;
using BookEcom.Api.Dtos.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PermissionsController(AppDbContext db, ILogger<PermissionsController> logger) : ControllerBase
{
    // GET /api/permissions
    [HttpGet]
    public async Task<IEnumerable<PermissionDto>> GetAll(CancellationToken ct)
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

        logger.LogInformation("GET /api/permissions — returning {Count} permissions", permissions.Count);
        return permissions;
    }
}
