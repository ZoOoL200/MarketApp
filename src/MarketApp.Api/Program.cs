using MarketApp.Api.ExceptionHandling;
using MarketApp.Api.Middleware;
using MarketApp.Application.Common.Security;
using MarketApp.Application.Extensions;
using MarketApp.Infrastructure.Extension;
using MarketApp.Infrastructure.Identity;
using MarketApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ApplicationServices();
builder.Services.AddMarketInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<DatabaseConflictHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthorization(options =>
{
    var stakeholderPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(AppRoles.Stakeholder)
        .Build();

    options.DefaultPolicy = stakeholderPolicy;
    options.FallbackPolicy = stakeholderPolicy;

    options.AddPolicy("AuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy("PasswordRecovery", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

app.UseMiddleware<IdentitySessionMiddleware>();

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

if (app.Configuration.GetValue<bool>("InitializeIdentity"))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "This setup command is currently restricted to Development.");
    }

    await using var scope = app.Services.CreateAsyncScope();

    var initializer =
        scope.ServiceProvider.GetRequiredService<IdentityInitializer>();

    var message = await initializer.InitializeAsync();

    Console.WriteLine(message);

    return;
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
