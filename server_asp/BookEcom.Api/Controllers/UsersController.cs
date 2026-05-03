using System.Security.Claims;
using BookEcom.Application.Auth.Authorization;
using BookEcom.Application.Dtos.Auth;
using BookEcom.Application.Dtos.Users;
using BookEcom.Application.Users;
using BookEcom.Domain.Auth;
using BookEcom.Domain.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController(IUserManagementService users) : ControllerBase
{
    // Bare [Authorize] inherited from the class — "any authenticated user".
    // Self-data endpoints don't need a permission gate because the JWT's sub
    // claim is the only identity they can ever operate on (Pattern 1 in
    // project_rbac.md).
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var callerId = CurrentUserId();
        if (callerId is null) return Unauthorized();
        return (await users.GetMeAsync(callerId.Value, ct)).ToActionResult();
    }

    [HttpGet]
    [HasPermission(PermissionNames.UsersRead)]
    public async Task<IReadOnlyList<UserResponse>> GetAll(CancellationToken ct) =>
        await users.GetAllAsync(ct);

    [HttpGet("{id:int}")]
    [HasPermission(PermissionNames.UsersRead)]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct) =>
        (await users.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    [HasPermission(PermissionNames.UsersCreate)]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest req, CancellationToken ct) =>
        (await users.CreateAsync(req, ct))
            .ToCreatedAtAction(this, nameof(GetById), u => new { id = u.Id });

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionNames.UsersDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var callerId = CurrentUserId();
        if (callerId is null) return Unauthorized();
        return (await users.DeleteAsync(id, callerId.Value, ct)).ToActionResult();
    }

    [HttpPut("{id:int}/roles")]
    [HasPermission(PermissionNames.UsersUpdate)]
    public async Task<ActionResult<UserResponse>> SetRoles(
        int id, SetUserRolesRequest req, CancellationToken ct) =>
        (await users.SetRolesAsync(id, req, ct)).ToActionResult();

    [HttpPut("{id:int}/permissions")]
    [HasPermission(PermissionNames.UsersUpdate)]
    public async Task<ActionResult<UserResponse>> SetPermissions(
        int id, SetUserPermissionsRequest req, CancellationToken ct) =>
        (await users.SetPermissionsAsync(id, req, ct)).ToActionResult();

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
