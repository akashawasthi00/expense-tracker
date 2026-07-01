namespace ExpenseTracker.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated (e.g. split amounts do not sum to the total).
/// Surfaced to the API as a 400/422 by the exception-handling middleware.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
