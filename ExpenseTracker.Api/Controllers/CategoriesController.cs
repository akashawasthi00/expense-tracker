using ExpenseTracker.Application.Contracts.Categories;
using ExpenseTracker.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

<<<<<<< HEAD
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categories) : ApiControllerBase
=======
[ApiController]
[Route("api/[controller]")]

//Testing for Ci/Cd 
public class CategoriesController : ControllerBase
>>>>>>> 1c2aa8a35895c5e3e4142c3618fc68a5f1096f86
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
