using ExpenseTracker.Application.Common.Results;

namespace ExpenseTracker.Application.Common.Abstractions;

/// <summary>A user as seen by the application layer (decoupled from ASP.NET Core Identity types).</summary>
public sealed record AuthUser(string Id, string FullName, string Email, IReadOnlyList<string> Roles);

/// <summary>
/// Wraps ASP.NET Core Identity (password hashing, user store) behind an abstraction so the
/// application layer never references Identity directly.
/// </summary>
public interface IIdentityService
{
    Task<Result<AuthUser>> RegisterAsync(string fullName, string email, string password, CancellationToken ct = default);

    Task<Result<AuthUser>> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);

    Task<AuthUser?> FindByIdAsync(string userId, CancellationToken ct = default);

    Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Resolves a set of user ids to users in one call (for filling display names on DTOs).</summary>
    Task<IReadOnlyDictionary<string, AuthUser>> GetUsersAsync(IEnumerable<string> userIds, CancellationToken ct = default);
}
