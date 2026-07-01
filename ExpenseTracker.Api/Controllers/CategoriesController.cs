using ExpenseTracker.Application.Contracts.Categories;
using ExpenseTracker.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

//Testing for Ci/Cd 
public class CategoriesController : ControllerBase
{
    /// <summary>Lists all expense categories.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        HandleResult(await categories.GetAllAsync(ct));

    /// <summary>Creates a new category.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct) =>
        HandleResult(await categories.CreateAsync(request, ct));
}
