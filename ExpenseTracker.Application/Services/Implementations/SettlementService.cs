using ExpenseTracker.Application.Common.Abstractions;
using ExpenseTracker.Application.Common.Errors;
using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Settlements;
using ExpenseTracker.Application.Services.Abstractions;
using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services.Implementations;

public sealed class SettlementService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IIdentityService identity,
    TimeProvider clock) : ISettlementService
{
    public async Task<Result<SettlementDto>> CreateAsync(
        Guid groupId, CreateSettlementRequest request, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var memberIds = await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        if (memberIds.Count == 0)
            return Error.NotFound("Group not found.");
        if (!memberIds.Contains(uid))
            return Error.Forbidden("You are not a member of this group.");
        if (request.Amount <= 0)
            return Error.Validation("Settlement amount must be greater than zero.");
        if (string.Equals(request.ToUserId, uid, StringComparison.Ordinal))
            return Error.Validation("You cannot record a settlement to yourself.");
        if (!memberIds.Contains(request.ToUserId))
            return Error.Validation("The payee must be a member of the group.");

        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            FromUserId = uid,                 // the caller records that they paid someone back
            ToUserId = request.ToUserId,
            Amount = Math.Round(request.Amount, 2),
            Note = request.Note?.Trim(),
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };

        db.Settlements.Add(settlement);
        await db.SaveChangesAsync(ct);

        var names = await identity.GetUsersAsync([settlement.FromUserId, settlement.ToUserId], ct);
        return Map(settlement, names);
    }

    public async Task<Result<IReadOnlyList<SettlementDto>>> ListAsync(Guid groupId, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        if (!await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == uid, ct))
            return Error.Forbidden("You are not a member of this group.");

        var settlements = await db.Settlements
            .Where(s => s.GroupId == groupId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);

        var ids = settlements.SelectMany(s => new[] { s.FromUserId, s.ToUserId });
        var names = await identity.GetUsersAsync(ids.Distinct(StringComparer.Ordinal), ct);

        IReadOnlyList<SettlementDto> dtos = settlements.Select(s => Map(s, names)).ToList();
        return Result.Success(dtos);
    }

    private static SettlementDto Map(Settlement s, IReadOnlyDictionary<string, AuthUser> names) =>
        new(s.Id, s.GroupId,
            s.FromUserId, NameOf(names, s.FromUserId),
            s.ToUserId, NameOf(names, s.ToUserId),
            s.Amount, s.Note, s.CreatedAtUtc);

    private static string NameOf(IReadOnlyDictionary<string, AuthUser> names, string userId) =>
        names.TryGetValue(userId, out var u) ? u.FullName : "(unknown)";
}
