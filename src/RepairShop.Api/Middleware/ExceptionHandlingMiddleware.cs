using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using RepairShop.Application.Common;
using RepairShop.Domain.Common;

namespace RepairShop.Api.Middleware;

/// <summary>
/// Single place where exceptions become HTTP responses, so controllers stay free
/// of try/catch.
/// <para>
/// The distinction that matters: a <see cref="DomainException"/> is the system
/// working — a caller asked for something the business rules forbid — and gets a
/// 400 with the rule's own message. Anything else is a defect, gets a generic
/// 500, and the details go to the log rather than to the client.
/// </para>
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogInformation(ex, "Business rule violated: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Business rule violated", ex.Message);
        }
        catch (NotFoundException ex)
        {
            logger.LogInformation("Resource not found: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Resource not found", ex.Message);
        }
        catch (ConflictException ex)
        {
            logger.LogInformation("Conflict: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "An unexpected error occurred. See the server log for details.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            // Too late to replace the response; swallowing here is better than
            // throwing a second exception out of the pipeline.
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
