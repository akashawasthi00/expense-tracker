using ExpenseTracker.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[Route("api/reports")]
public sealed class ReportsController(IReportService reports) : ApiControllerBase
{
    /// <summary>Total spend grouped by category. Scope: a group (groupId) or the caller's personal expenses.</summary>
    [HttpGet("by-category")]
    public async Task<IActionResult> ByCategory(
        [FromQuery] Guid? groupId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        HandleResult(await reports.ByCategoryAsync(groupId, from, to, ct));

    /// <summary>Total spend per month for a year. Scope: a group (groupId) or the caller's personal expenses.</summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly(
        [FromQuery] int year, [FromQuery] Guid? groupId, CancellationToken ct) =>
        HandleResult(await reports.MonthlyAsync(year, groupId, ct));
}
