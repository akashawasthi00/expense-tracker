using ExpenseTracker.Application.Common.Abstractions;
using ExpenseTracker.Application.Common.Errors;
using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Auth;
using ExpenseTracker.Application.Services.Abstractions;

namespace ExpenseTracker.Application.Services.Implementations;

public sealed class AuthService(
    IIdentityService identity,
    IJwtTokenService tokens,
    ICurrentUser currentUser) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var result = await identity.RegisterAsync(request.FullName, request.Email, request.Password, ct);
        return result.IsSuccess ? IssueToken(result.Value!) : Result.Failure<AuthResponse>(result.Error!);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var result = await identity.ValidateCredentialsAsync(request.Email, request.Password, ct);
        return result.IsSuccess ? IssueToken(result.Value!) : Result.Failure<AuthResponse>(result.Error!);
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Unauthorized("Not authenticated.");

        var user = await identity.FindByIdAsync(currentUser.UserId!, ct);
        return user is null
            ? Error.NotFound("User not found.")
            : new UserDto(user.Id, user.FullName, user.Email);
    }

    private Result<AuthResponse> IssueToken(AuthUser user)
    {
        var (token, expiresAt) = tokens.CreateToken(user.Id, user.Email, user.Roles);
        var dto = new UserDto(user.Id, user.FullName, user.Email);
        return new AuthResponse(token, expiresAt, dto);
    }
}
