using ExpenseTracker.Application.Common.Abstractions;
using ExpenseTracker.Application.Contracts.Expenses;
using ExpenseTracker.Application.Services.Implementations;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Tests.Services;

/// <summary>
/// End-to-end (service + EF) check of the headline flow: a group expense is split, and the group's
/// balances + suggested settlements come out correct.
/// </summary>
public class GroupExpenseFlowTests
{
    private static AppDbContext NewDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static Guid SeedGroup(AppDbContext db)
    {
        var groupId = Guid.NewGuid();
        db.Groups.Add(new Group
        {
            Id = groupId,
            Name = "Goa Trip",
            CreatedByUserId = "a",
            CreatedAtUtc = DateTime.UtcNow,
            Members =
            {
                new GroupMember { Id = Guid.NewGuid(), UserId = "a", Role = GroupRole.Admin, JoinedAtUtc = DateTime.UtcNow },
                new GroupMember { Id = Guid.NewGuid(), UserId = "b", Role = GroupRole.Member, JoinedAtUtc = DateTime.UtcNow },
                new GroupMember { Id = Guid.NewGuid(), UserId = "c", Role = GroupRole.Member, JoinedAtUtc = DateTime.UtcNow }
            }
        });
        db.SaveChanges();
        return groupId;
    }

    [Fact]
    public async Task Equal_group_expense_splits_and_produces_correct_balances()
    {
        using var db = NewDb(nameof(Equal_group_expense_splits_and_produces_correct_balances));
        var groupId = SeedGroup(db);

        var identity = new FakeIdentityService();
        var clock = TimeProvider.System;

        // "a" pays 90, split equally across a/b/c.
        var expenseService = new ExpenseService(db, new FakeCurrentUser("a"), identity, clock);
        var create = await expenseService.CreateAsync(new CreateExpenseRequest(
            CategoryId: 1,
            Description: "Hotel",
            Amount: 90m,
            GroupId: groupId,
            SplitType: SplitType.Equal,
            Participants: [new ExpenseParticipantInput("a"), new ExpenseParticipantInput("b"), new ExpenseParticipantInput("c")]),
            CancellationToken.None);

        Assert.True(create.IsSuccess, create.Error?.Message);
        Assert.Equal(3, create.Value!.Shares.Count);
        Assert.All(create.Value.Shares, s => Assert.Equal(30m, s.Amount));

        // Balances: a is owed 60 (paid 90, owes 30); b and c each owe 30.
        var groupService = new GroupService(db, new FakeCurrentUser("a"), identity, clock);
        var balances = await groupService.GetBalancesAsync(groupId, CancellationToken.None);

        Assert.True(balances.IsSuccess, balances.Error?.Message);
        var net = balances.Value!.Balances.ToDictionary(b => b.UserId, b => b.Net);
        Assert.Equal(60m, net["a"]);
        Assert.Equal(-30m, net["b"]);
        Assert.Equal(-30m, net["c"]);

        // Two suggested settlements, both flowing to "a", totalling 60.
        Assert.Equal(2, balances.Value.SuggestedSettlements.Count);
        Assert.All(balances.Value.SuggestedSettlements, t => Assert.Equal("a", t.ToUserId));
        Assert.Equal(60m, balances.Value.SuggestedSettlements.Sum(t => t.Amount));
    }

    [Fact]
    public async Task Non_member_cannot_create_a_group_expense()
    {
        using var db = NewDb(nameof(Non_member_cannot_create_a_group_expense));
        var groupId = SeedGroup(db);

        // "z" is not a member of the group.
        var service = new ExpenseService(db, new FakeCurrentUser("z"), new FakeIdentityService(), TimeProvider.System);
        var result = await service.CreateAsync(new CreateExpenseRequest(
            CategoryId: 1, Description: "Sneaky", Amount: 10m, GroupId: groupId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    // ----- test doubles -----

    private sealed class FakeCurrentUser(string id) : ICurrentUser
    {
        public string? UserId => id;
        public bool IsAuthenticated => true;
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        private static readonly Dictionary<string, AuthUser> Users = new()
        {
            ["a"] = new AuthUser("a", "Akash", "akash@example.com", []),
            ["b"] = new AuthUser("b", "Riya", "riya@example.com", []),
            ["c"] = new AuthUser("c", "Sam", "sam@example.com", [])
        };

        public Task<IReadOnlyDictionary<string, AuthUser>> GetUsersAsync(IEnumerable<string> userIds, CancellationToken ct = default)
        {
            IReadOnlyDictionary<string, AuthUser> map = userIds
                .Where(Users.ContainsKey)
                .Distinct()
                .ToDictionary(id => id, id => Users[id]);
            return Task.FromResult(map);
        }

        public Task<AuthUser?> FindByIdAsync(string userId, CancellationToken ct = default) =>
            Task.FromResult(Users.GetValueOrDefault(userId));

        public Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.Values.FirstOrDefault(u => u.Email == email));

        public Task<Application.Common.Results.Result<AuthUser>> RegisterAsync(string fullName, string email, string password, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Application.Common.Results.Result<AuthUser>> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
