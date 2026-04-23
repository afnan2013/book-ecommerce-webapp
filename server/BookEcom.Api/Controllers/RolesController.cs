using BookEcom.Api.Application.Roles;
using BookEcom.Api.Common.Results;
using BookEcom.Api.Dtos.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RolesController(IRoleManagementService roles) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<RoleResponse>> GetAll(CancellationToken ct) =>
        await roles.GetAllAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleResponse>> GetById(int id, CancellationToken ct) =>
        (await roles.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create(CreateRoleRequest req, CancellationToken ct) =>
        (await roles.CreateAsync(req, ct))
            .ToCreatedAtAction(this, nameof(GetById), r => new { id = r.Id });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRoleRequest req, CancellationToken ct) =>
        (await roles.UpdateAsync(id, req, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        (await roles.DeleteAsync(id, ct)).ToActionResult();

    [HttpPut("{id:int}/permissions")]
    public async Task<ActionResult<RoleResponse>> SetPermissions(
        int id, SetRolePermissionsRequest req, CancellationToken ct) =>
        (await roles.SetPermissionsAsync(id, req, ct)).ToActionResult();
}
