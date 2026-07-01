namespace ExpenseTracker.Application.Contracts.Settlements;

public sealed record CreateSettlementRequest(string ToUserId, decimal Amount, string? Note = null);

public sealed record SettlementDto(
    Guid Id,
    Guid GroupId,
    string FromUserId,
    string FromName,
    string ToUserId,
    string ToName,
    decimal Amount,
    string? Note,
    DateTime CreatedAtUtc);
