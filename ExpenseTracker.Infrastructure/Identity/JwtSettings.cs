namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Bound from the "Jwt" configuration section. The signing key must come from a secret in production.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ExpenseTracker";
    public string Audience { get; set; } = "ExpenseTracker.Clients";
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 120;
}
