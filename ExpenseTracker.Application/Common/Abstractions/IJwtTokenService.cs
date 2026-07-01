namespace ExpenseTracker.Application.Common.Abstractions;

/// <summary>Issues signed JWT access tokens. Implemented in Infrastructure.</summary>
public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(string userId, string email, IEnumerable<string> roles);
}
