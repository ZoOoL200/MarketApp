using System.Text.Json;
using MarketApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MarketApp.Api.Tests;

public sealed class DatabaseConflictHandlerTests
{
    [Theory]
    [InlineData("40001", 0)]
    [InlineData("40001", 1)]
    [InlineData("40001", 2)]
    [InlineData("40P01", 2)]
    [InlineData("23505", 2)]
    [InlineData("23503", 2)]
    public async Task Database_conflicts_return_409_even_inside_provider_wrappers(string sqlState, int wrappers)
    {
        Exception error = new PostgresException("Private database detail", "ERROR", "ERROR", sqlState);
        if (wrappers >= 1) error = new DbUpdateException("Save failed", error);
        if (wrappers >= 2) error = new InvalidOperationException("Transient provider failure", error);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        using var body = new MemoryStream(); context.Response.Body = body;

        Assert.True(await new DatabaseConflictHandler().TryHandleAsync(context, error, CancellationToken.None));
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body);
        Assert.Equal(409, json.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("Private database detail", json.RootElement.GetRawText());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unrelated_errors_are_left_for_the_global_handler(bool databaseError)
    {
        Exception inner = databaseError
            ? new PostgresException("Undefined table", "ERROR", "ERROR", "42P01")
            : new ArgumentException("Invalid argument");
        var error = new InvalidOperationException("Wrapper", new Exception("Nested", inner));
        var context = new DefaultHttpContext();
        using var body = new MemoryStream(); context.Response.Body = body;

        Assert.False(await new DatabaseConflictHandler().TryHandleAsync(context, error, CancellationToken.None));
        Assert.Equal(0L, body.Length);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
