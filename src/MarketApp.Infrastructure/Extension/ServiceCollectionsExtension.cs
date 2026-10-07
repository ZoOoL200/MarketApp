using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Infrastructure.Email;
using MarketApp.Infrastructure.Identity;
using MarketApp.Infrastructure.Persistence;
using MarketApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;


namespace MarketApp.Infrastructure.Extension;

public static class ServiceCollectionsExtension
{
    public static IServiceCollection AddMarketInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("MarketDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'MarketDatabase' was not found.");
        // Add DbContext with PostgreSQL provider
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        // Add data protection services
        services.AddDataProtection();
        // Add Identity services
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;

            options.Password.RequiredLength = 4;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars =0;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

 

        // Add JWT authentication
        var jwtSettings = configuration
        .GetRequiredSection("Jwt")
        .Get<JwtSettings>()
        ?? throw new InvalidOperationException(
            "JWT configuration is missing.");

            var signingKey = jwtSettings.CreateSigningKey();

            services.AddSingleton(jwtSettings);
            services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.SaveToken = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        RequireSignedTokens = true,
                        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },

                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),

                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role
                    };
                });

        // Add repositories and unit of work
        services.AddScoped(typeof(IGeneralRepository<>),typeof(GeneralRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IdentityInitializer>();
        // Add custom identity service
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();

        services.AddScoped<IApplicationEmailSender, DevelopmentEmailSender>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(1);
        });

        services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IBranchAccessService, BranchAccessService>();

        services.AddProductPhotos(configuration);

        return services;
    }
}
