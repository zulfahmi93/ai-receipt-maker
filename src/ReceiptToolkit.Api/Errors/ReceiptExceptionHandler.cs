using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReceiptToolkit.Contracts;

namespace ReceiptToolkit.Api.Errors;

/// <summary>
///   Translates uncaught exceptions into RFC7807 <see cref="ProblemDetails"/>
///   responses. <see cref="ReceiptValidationException"/> becomes a 400 with the
///   validator's <c>errors[]</c> surfaced via the <c>extensions</c> bag; all other
///   exceptions become a 500 with a <c>traceId</c> correlation key tied to the
///   active <see cref="Activity"/> (or the request's <c>HttpContext.TraceIdentifier</c>
///   when no Activity is running).
/// </summary>
internal sealed class ReceiptExceptionHandler : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        ProblemDetails problem = exception switch
        {
            ReceiptValidationException validation => BuildValidationProblem(validation),
            JsonException jsonEx => BuildMalformedJsonProblem(jsonEx),
            BadHttpRequestException badRequest => BuildBadRequestProblem(badRequest),
            _ => BuildGenerationProblem(httpContext, exception),
        };

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return true;
    }

    private static ProblemDetails BuildValidationProblem(ReceiptValidationException ex)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807#section-3.1",
            Title = "Receipt validation failed.",
            Status = StatusCodes.Status400BadRequest,
            Detail = ex.Message,
        };

        var errors = ex.Errors
            .Select(e => new { field = e.Field, message = e.Message })
            .ToArray();
        problem.Extensions["errors"] = errors;
        return problem;
    }

    private static ProblemDetails BuildMalformedJsonProblem(JsonException ex)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807#section-3.1",
            Title = "Malformed JSON request body.",
            Status = StatusCodes.Status400BadRequest,
            Detail = ex.Message,
        };
    }

    private static ProblemDetails BuildBadRequestProblem(BadHttpRequestException ex)
    {
        // ASP.NET wraps body-binder failures in BadHttpRequestException with StatusCode=400.
        // The inner exception is typically JsonException for malformed JSON; surface either as RFC7807.
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807#section-3.1",
            Title = "Bad request.",
            Status = ex.StatusCode is >= 400 and < 500 ? ex.StatusCode : StatusCodes.Status400BadRequest,
            Detail = ex.Message,
        };
    }

    private static ProblemDetails BuildGenerationProblem(HttpContext context, Exception exception)
    {
        string correlationId = Activity.Current?.Id ?? context.TraceIdentifier;
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807#section-3.1",
            Title = "Receipt generation failed.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = exception.Message,
        };
        problem.Extensions["traceId"] = correlationId;
        return problem;
    }
}
