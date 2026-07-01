using ExpenseTracker.Application.Contracts.Expenses;
using ExpenseTracker.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[Route("api/expenses")]
public sealed class ExpensesController(IExpenseService expenses) : ApiControllerBase
{
    /// <summary>Creates a personal expense (no GroupId) or a split group expense.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateExpenseRequest request, CancellationToken ct)
    {
        var result = await expenses.CreateAsync(request, ct);
        return result.IsSuccess
            ? HandleCreated(result, $"/api/expenses/{result.Value!.Id}")
            : HandleResult(result);
    }

    /// <summary>Gets a single expense the caller can see.</summary>
    [HttpGet("{expenseId:guid}")]
    public async Task<IActionResult> GetById(Guid expenseId, CancellationToken ct) =>
        HandleResult(await expenses.GetByIdAsync(expenseId, ct));

    /// <summary>Lists expenses visible to the caller, filtered and paged.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ExpenseQuery query, CancellationToken ct) =>
        HandleResult(await expenses.ListAsync(query, ct));

    /// <summary>Updates an expense (creator only); recomputes the split for group expenses.</summary>
    [HttpPut("{expenseId:guid}")]
    public async Task<IActionResult> Update(Guid expenseId, UpdateExpenseRequest request, CancellationToken ct) =>
        HandleResult(await expenses.UpdateAsync(expenseId, request, ct));

    /// <summary>Deletes an expense (creator only).</summary>
    [HttpDelete("{expenseId:guid}")]
    public async Task<IActionResult> Delete(Guid expenseId, CancellationToken ct) =>
        HandleResult(await expenses.DeleteAsync(expenseId, ct));
}
