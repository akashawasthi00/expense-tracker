using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Auth;

namespace ExpenseTracker.Application.Services.Abstractions;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<UserDto>> GetCurrentUserAsync(CancellationToken ct = default);
}
