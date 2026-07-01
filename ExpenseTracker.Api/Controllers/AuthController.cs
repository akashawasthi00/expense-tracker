using ExpenseTracker.Application.Contracts.Auth;
using ExpenseTracker.Application.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    /// <summary>Registers a new user and returns a JWT.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) =>
        HandleResult(await auth.RegisterAsync(request, ct));

    /// <summary>Authenticates a user and returns a JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        HandleResult(await auth.LoginAsync(request, ct));

    /// <summary>Returns the profile of the authenticated caller.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        HandleResult(await auth.GetCurrentUserAsync(ct));
}
