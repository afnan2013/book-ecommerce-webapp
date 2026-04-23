using BookEcom.Application.Permissions;
using BookEcom.Application.Dtos.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PermissionsController(IPermissionService permissions) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<PermissionDto>> GetAll(CancellationToken ct) =>
        await permissions.GetAllAsync(ct);
}
