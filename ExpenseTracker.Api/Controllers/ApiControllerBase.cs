using ExpenseTracker.Application.Common.Errors;
using ExpenseTracker.Application.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

/// <summary>
/// Base for all API controllers. Translates the application's <see cref="Result"/>/<see cref="Error"/>
/// into HTTP responses so individual actions stay thin. Authenticated by default.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult(Result result) =>
        result.IsSuccess ? NoContent() : ToProblem(result.Error!);

    protected IActionResult HandleResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error!);

    protected IActionResult HandleCreated<T>(Result<T> result, string routeValuePath) =>
        result.IsSuccess ? Created(routeValuePath, result.Value) : ToProblem(result.Error!);

    private ObjectResult ToProblem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(detail: error.Message, statusCode: status, title: error.Code);
    }
}
