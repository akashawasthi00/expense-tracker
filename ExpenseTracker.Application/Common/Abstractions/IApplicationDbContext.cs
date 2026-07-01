using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Common.Abstractions;

/// <summary>
/// The persistence surface the application layer is allowed to see. The concrete EF Core
/// <c>AppDbContext</c> lives in Infrastructure and implements this — so services depend on an
/// abstraction, not on the database technology.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Group> Groups { get; }
    DbSet<GroupMember> GroupMembers { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<ExpenseShare> ExpenseShares { get; }
    DbSet<Settlement> Settlements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
