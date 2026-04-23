using BookEcom.Application.Auth;
using BookEcom.Domain.Common.Results;
using BookEcom.Application.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest req, CancellationToken ct) =>
        (await auth.RegisterAsync(req, ct)).ToActionResult();

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct) =>
        (await auth.LoginAsync(req, ct)).ToActionResult();
}
