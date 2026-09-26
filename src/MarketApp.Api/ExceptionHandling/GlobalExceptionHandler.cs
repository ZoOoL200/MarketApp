using System.Diagnostics;
using MarketApp.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id
            ?? httpContext.TraceIdentifier;

        var isConflict = exception is ConflictException;

        var statusCode = isConflict
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;

        if (isConflict)
        {
            _logger.LogWarning(
                "Data conflict. TraceId: {TraceId}. Message: {Message}",
                traceId,
                exception.Message);
        }
        else
        {
            _logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                traceId);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,

            Title = isConflict
                ? "Data conflict."
                : "An unexpected error occurred.",

            Detail = isConflict
                ? exception.Message
                : "The request could not be completed. " +
                  "Contact support if the problem continues.",

            Instance = httpContext.Request.Path.Value
        };

        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}