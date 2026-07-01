namespace ExpenseTracker.Domain.Enums;

/// <summary>
/// A member's role within a group. Admins can manage membership; members can only participate.
/// </summary>
public enum GroupRole
{
    Member = 0,
    Admin = 1
}
