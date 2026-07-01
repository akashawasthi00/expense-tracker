namespace ExpenseTracker.Application.Contracts.Auth;

public sealed record RegisterRequest(string FullName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(string Id, string FullName, string Email);

public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserDto User);
