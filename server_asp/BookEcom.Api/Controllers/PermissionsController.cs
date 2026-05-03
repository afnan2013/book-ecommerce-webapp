using BookEcom.Application.Auth.Authorization;
using BookEcom.Application.Dtos.Permissions;
using BookEcom.Application.Permissions;
using BookEcom.Domain.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PermissionsController(IPermissionService permissions) : ControllerBase
{
    // The permission catalog is consumed by anyone editing roles or per-user
    // direct permissions. Gating with RolesRead means user-permission editors
    // also need RolesRead — practical for now (admin tier overlaps); switch to
    // a HasAnyPermission attribute if it becomes painful.
    [HttpGet]
    [HasPermission(PermissionNames.RolesRead)]
    public async Task<IReadOnlyList<PermissionDto>> GetAll(CancellationToken ct) =>
        await permissions.GetAllAsync(ct);
}
