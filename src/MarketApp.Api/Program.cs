using MarketApp.Api.Configuration;
using MarketApp.Api.Documentation;
using MarketApp.Api.ExceptionHandling;
using MarketApp.Api.Middleware;
using MarketApp.Application.Common.Security;
using MarketApp.Application.Extensions;
using MarketApp.Infrastructure.Extension;
using MarketApp.Infrastructure.Identity;
using MarketApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ApplicationServices();
builder.Services.AddMarketInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddMarketOpenApi();
builder.Services.AddMarketOperations(builder.Configuration, builder.Environment);
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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("ManagerWeb");

app.UseRateLimiter();

app.UseAuthentication();

app.UseMiddleware<IdentitySessionMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapOpenApi().RequireAuthorization();
app.MapMarketHealth();

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
