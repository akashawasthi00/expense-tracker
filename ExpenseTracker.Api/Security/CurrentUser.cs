using System.Security.Claims;
using ExpenseTracker.Application.Common.Abstractions;

namespace ExpenseTracker.Api.Security;

/// <summary>Resolves the caller's identity from the validated JWT on the current HTTP request.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? accessor.HttpContext?.User.FindFirstValue("sub");

    public bool IsAuthenticated =>
        accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
