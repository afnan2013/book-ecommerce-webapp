using BookEcom.Application.Auth.Authorization;
using BookEcom.Application.Dtos.Roles;
using BookEcom.Application.Roles;
using BookEcom.Domain.Auth;
using BookEcom.Domain.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RolesController(IRoleManagementService roles) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionNames.RolesRead)]
    public async Task<IReadOnlyList<RoleResponse>> GetAll(CancellationToken ct) =>
        await roles.GetAllAsync(ct);

    [HttpGet("{id:int}")]
    [HasPermission(PermissionNames.RolesRead)]
    public async Task<ActionResult<RoleResponse>> GetById(int id, CancellationToken ct) =>
        (await roles.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    [HasPermission(PermissionNames.RolesCreate)]
    public async Task<ActionResult<RoleResponse>> Create(CreateRoleRequest req, CancellationToken ct) =>
        (await roles.CreateAsync(req, ct))
            .ToCreatedAtAction(this, nameof(GetById), r => new { id = r.Id });

    [HttpPut("{id:int}")]
    [HasPermission(PermissionNames.RolesUpdate)]
    public async Task<IActionResult> Update(int id, UpdateRoleRequest req, CancellationToken ct) =>
        (await roles.UpdateAsync(id, req, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionNames.RolesDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        (await roles.DeleteAsync(id, ct)).ToActionResult();

    [HttpPut("{id:int}/permissions")]
    [HasPermission(PermissionNames.RolesUpdate)]
    public async Task<ActionResult<RoleResponse>> SetPermissions(
        int id, SetRolePermissionsRequest req, CancellationToken ct) =>
        (await roles.SetPermissionsAsync(id, req, ct)).ToActionResult();
}
