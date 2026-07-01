namespace ExpenseTracker.Domain.Common;

/// <summary>
/// Base type for entities that track who created them and when.
/// Audit fields are populated by the persistence layer (SaveChanges interceptor / services).
/// </summary>
public abstract class AuditableEntity
{
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Identity user id (string) of the creator.</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }
}
