using ExpenseTracker.Application.Common.Abstractions;
using ExpenseTracker.Application.Common.Errors;
using ExpenseTracker.Application.Common.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Adapts ASP.NET Core Identity's <see cref="UserManager{T}"/> to the application's <see cref="IIdentityService"/>.</summary>
public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<Result<AuthUser>> RegisterAsync(
        string fullName, string email, string password, CancellationToken ct = default)
    {
        email = email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            return Error.Conflict("A user with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName.Trim()
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return Error.Validation(string.Join(" ", result.Errors.Select(e => e.Description)));

        return await ToAuthUserAsync(user);
    }

    public async Task<Result<AuthUser>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return Error.Unauthorized("Invalid email or password.");

        return await ToAuthUserAsync(user);
    }

    public async Task<AuthUser?> FindByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : await ToAuthUserAsync(user);
    }

    public async Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        return user is null ? null : await ToAuthUserAsync(user);
    }

    public async Task<IReadOnlyDictionary<string, AuthUser>> GetUsersAsync(
        IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count == 0)
            return new Dictionary<string, AuthUser>();

        // Bulk lookup for display names; roles aren't needed here, so we skip the per-user role query.
        var users = await userManager.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(ct);

        return users.ToDictionary(
            u => u.Id,
            u => new AuthUser(u.Id, u.FullName, u.Email ?? string.Empty, Array.Empty<string>()),
            StringComparer.Ordinal);
    }

    private async Task<AuthUser> ToAuthUserAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AuthUser(user.Id, user.FullName, user.Email ?? string.Empty, roles.ToList());
    }
}
