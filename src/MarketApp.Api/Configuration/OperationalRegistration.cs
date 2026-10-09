using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using MarketApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
namespace MarketApp.Api.Configuration;
public static class OperationalRegistration
{
    public static IServiceCollection AddMarketOperations(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        Validate(configuration, environment);
        var protection = services.AddDataProtection().SetApplicationName("MarketApp");
        var directory = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(directory)) protection.PersistKeysToFileSystem(new DirectoryInfo(directory));
        var certificate = configuration["DataProtection:CertificatePath"];
        if (!string.IsNullOrWhiteSpace(certificate)) protection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(certificate,
            configuration["DataProtection:CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet));
        services.AddCors(options => options.AddPolicy("ManagerWeb", policy =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }));
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? []) options.KnownProxies.Add(IPAddress.Parse(proxy));
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("Authentication", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
            options.AddPolicy("PasswordRecovery", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true }));
        });
        return services;
    }
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        foreach (var origin in configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.GetLeftPart(UriPartial.Authority) != origin || !string.IsNullOrEmpty(uri.UserInfo) ||
                (uri.Scheme != "https" && !(environment.IsDevelopment() && uri.Scheme == "http")))
                throw new InvalidOperationException("Cors:AllowedOrigins must be exact HTTPS origins without paths or trailing slashes; HTTP is allowed only in Development.");
        foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            if (!IPAddress.TryParse(proxy, out _)) throw new InvalidOperationException("ReverseProxy:KnownProxies must contain IP addresses.");
        if (environment.IsDevelopment()) return;
        if (!string.Equals(configuration["Email:Provider"], "Smtp", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(configuration["Email:Host"]) ||
            !MailAddress.TryCreate(configuration["Email:From"], out _) || configuration.GetValue("Email:Port", 587) is < 1 or > 65535)
            throw new InvalidOperationException("Outside Development configure Email:Provider=Smtp, Host, From, and a valid Port.");
        if (string.IsNullOrWhiteSpace(configuration["DataProtection:KeysPath"]) || !Path.IsPathFullyQualified(configuration["DataProtection:KeysPath"]!))
            throw new InvalidOperationException("DataProtection:KeysPath must be an absolute persistent directory outside Development.");
        var hosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(hosts) || hosts.Split(';', StringSplitOptions.TrimEntries).Any(h => h.Length == 0 || h.Contains('*')))
            throw new InvalidOperationException("Outside Development set AllowedHosts to explicit API host names separated by semicolons.");
        if (string.Equals(configuration["PhotoStorage:Provider"] ?? "Local", "Local", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(configuration["PhotoStorage:LocalRoot"]) || !Path.IsPathFullyQualified(configuration["PhotoStorage:LocalRoot"]!)))
            throw new InvalidOperationException("Local photos require an absolute persistent PhotoStorage:LocalRoot outside Development.");
    }
    public static void MapMarketHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" })).Produces<MarketApp.Api.Models.HealthResponse>().AllowAnonymous();
        endpoints.MapGet("/health/ready", async (AppDbContext db, CancellationToken ct) =>
            await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "Ready" }) : Results.StatusCode(503)).Produces<MarketApp.Api.Models.HealthResponse>().Produces(503).RequireAuthorization();
    }
}
