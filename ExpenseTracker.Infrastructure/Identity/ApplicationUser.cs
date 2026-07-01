using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>
/// The Identity user. Lives in Infrastructure so ASP.NET Core Identity never leaks into the
/// domain/application layers — those reference users by their string id only.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
