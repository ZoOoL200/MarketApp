using MarketApp.Api.ExceptionHandling;
using MarketApp.Application.Extensions;
using MarketApp.Infrastructure.Extension;
using MarketApp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ApplicationServices();
builder.Services.AddMarketInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/health/database", async (AppDbContext db) =>
    {
        var connected = await db.Database.CanConnectAsync();

        return connected
            ? Results.Ok(new { database = "Connected" })
            : Results.Problem(
                detail: "Could not connect to PostgreSQL.",
                statusCode: 503);
    });
    app.MapGet("/debug/errors/conflict", ThrowTestConflict);
    app.MapGet("/debug/errors/unexpected", ThrowTestUnexpected);
}


app.Run();
static IResult ThrowTestConflict()
{
    throw new MarketApp.Application.Common.Exceptions
        .ConflictException("This is a test conflict.");
}

static IResult ThrowTestUnexpected()
{
    throw new InvalidOperationException(
        "Technical test details that should appear only in logs.");
}
