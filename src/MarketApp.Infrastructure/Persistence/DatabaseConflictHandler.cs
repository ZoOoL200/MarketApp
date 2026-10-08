using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace MarketApp.Infrastructure.Persistence;

// Handle serialization failures that can arise while reading or committing,
// as well as the failures translated by UnitOfWork during SaveChanges.
public sealed class DatabaseConflictHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var cause = exception as PostgresException ?? exception.InnerException as PostgresException;
        if (cause?.SqlState is not ("40001" or "40P01" or "23505" or "23503")) return false;
        context.Response.StatusCode = 409;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 409, Title = "Data conflict.",
            Detail = "The data changed or conflicts with an existing record. For a retry, reuse the same client identifier and unchanged request.",
            Instance = context.Request.Path
        }, cancellationToken);
        return true;
    }
}
