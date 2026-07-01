using ExpenseTracker.Application.Contracts.Settlements;
using ExpenseTracker.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[Route("api/groups/{groupId:guid}/settlements")]
public sealed class SettlementsController(ISettlementService settlements) : ApiControllerBase
{
    /// <summary>Records that the caller paid another member back.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(Guid groupId, CreateSettlementRequest request, CancellationToken ct) =>
        HandleResult(await settlements.CreateAsync(groupId, request, ct));

    /// <summary>Lists the group's settlements, newest first.</summary>
    [HttpGet]
    public async Task<IActionResult> List(Guid groupId, CancellationToken ct) =>
        HandleResult(await settlements.ListAsync(groupId, ct));
}
