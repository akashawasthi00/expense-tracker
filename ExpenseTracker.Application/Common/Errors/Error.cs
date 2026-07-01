namespace ExpenseTracker.Application.Common.Errors;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
    Failure
}

/// <summary>A structured, transport-agnostic failure. The API maps <see cref="Type"/> to an HTTP status.</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string message) => new("validation", message, ErrorType.Validation);
    public static Error NotFound(string message) => new("not_found", message, ErrorType.NotFound);
    public static Error Conflict(string message) => new("conflict", message, ErrorType.Conflict);
    public static Error Forbidden(string message) => new("forbidden", message, ErrorType.Forbidden);
    public static Error Unauthorized(string message) => new("unauthorized", message, ErrorType.Unauthorized);
    public static Error Failure(string message) => new("failure", message, ErrorType.Failure);
}
