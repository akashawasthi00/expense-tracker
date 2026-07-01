using ExpenseTracker.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Middleware;

/// <summary>
/// Last line of defense: converts unhandled exceptions into RFC-7807 ProblemDetails so the API never
/// leaks stack traces. Expected failures are returned as <c>Result</c> values instead and never reach here.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            // A domain invariant slipped through to the boundary -> 400.
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "domain_error", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "server_error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        });
    }
}
