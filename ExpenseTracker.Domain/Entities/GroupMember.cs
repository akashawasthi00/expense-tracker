using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Domain.Entities;

/// <summary>
/// Join entity linking a user (Identity user id) to a <see cref="Group"/> with a role.
/// Unique per (GroupId, UserId).
/// </summary>
public class GroupMember
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    /// <summary>Identity user id (string).</summary>
    public string UserId { get; set; } = string.Empty;

    public GroupRole Role { get; set; } = GroupRole.Member;

    public DateTime JoinedAtUtc { get; set; }
}
