using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Settlements;

namespace ExpenseTracker.Application.Services.Abstractions;

public interface ISettlementService
{
    Task<Result<SettlementDto>> CreateAsync(Guid groupId, CreateSettlementRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SettlementDto>>> ListAsync(Guid groupId, CancellationToken ct = default);
}
