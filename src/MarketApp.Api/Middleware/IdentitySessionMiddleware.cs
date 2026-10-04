using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace MarketApp.Api.Middleware;

public class IdentitySessionMiddleware
{
    private readonly RequestDelegate _next;

    public IdentitySessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIdentityService identityService)
    {
        var allowsAnonymous = context.GetEndpoint()?
            .Metadata.GetMetadata<IAllowAnonymous>() is not null;

        if (!allowsAnonymous
            && context.User.Identity?.IsAuthenticated == true)
        {
            var principal =
                await identityService.ValidateSessionAsync(context.User);

            if (principal is null)
            {
                await context.ChallengeAsync();
                return;
            }

            context.User = principal;
        }

        await _next(context);
    }
}