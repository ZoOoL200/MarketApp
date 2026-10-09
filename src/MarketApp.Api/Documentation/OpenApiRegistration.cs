using Microsoft.OpenApi;
namespace MarketApp.Api.Documentation;

public static class OpenApiRegistration
{
    public static IServiceCollection AddMarketOpenApi(this IServiceCollection services)
    {
        return services.AddOpenApi(options => options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Info.Title = "MarketApp API";
            document.Info.Version = "v1";
            document.Info.Description = "Multi-branch inventory, pricing, sales and management API. Use a bearer access token. Branch assignments and role checks also apply.";
            document.Servers = [new OpenApiServer { Url = "/" }];
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" };
            document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
            string[] anonymous = ["/api/auth/login", "/api/auth/refresh", "/api/auth/forgot-password",
                "/api/auth/reset-password", "/api/auth/confirm-email", "/health/live"];
            foreach (var path in document.Paths)
                if (anonymous.Contains(path.Key) && path.Value.Operations is not null)
                    foreach (var operation in path.Value.Operations.Values) operation.Security = [];
            return Task.CompletedTask;
        }));
    }
}
