namespace ExpenseTracker.Application.Common.Abstractions;

/// <summary>
/// Ambient information about the caller, resolved from the JWT by the API layer.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Identity user id of the caller, or null when unauthenticated.</summary>
    string? UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>The caller's id, or throws if there is no authenticated user (guards against misuse).</summary>
    string RequireUserId() =>
        UserId ?? throw new InvalidOperationException("No authenticated user is available in this context.");
}
