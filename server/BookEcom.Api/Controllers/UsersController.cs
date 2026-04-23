using System.Security.Claims;
using BookEcom.Api.Application.Users;
using BookEcom.Domain.Common.Results;
using BookEcom.Api.Dtos.Auth;
using BookEcom.Api.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController(IUserManagementService users) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var callerId = CurrentUserId();
        if (callerId is null) return Unauthorized();
        return (await users.GetMeAsync(callerId.Value, ct)).ToActionResult();
    }

    [HttpGet]
    public async Task<IReadOnlyList<UserResponse>> GetAll(CancellationToken ct) =>
        await users.GetAllAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct) =>
        (await users.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest req, CancellationToken ct) =>
        (await users.CreateAsync(req, ct))
            .ToCreatedAtAction(this, nameof(GetById), u => new { id = u.Id });

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var callerId = CurrentUserId();
        if (callerId is null) return Unauthorized();
        return (await users.DeleteAsync(id, callerId.Value, ct)).ToActionResult();
    }

    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult<UserResponse>> SetRoles(
        int id, SetUserRolesRequest req, CancellationToken ct) =>
        (await users.SetRolesAsync(id, req, ct)).ToActionResult();

    [HttpPut("{id:int}/permissions")]
    public async Task<ActionResult<UserResponse>> SetPermissions(
        int id, SetUserPermissionsRequest req, CancellationToken ct) =>
        (await users.SetPermissionsAsync(id, req, ct)).ToActionResult();

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
