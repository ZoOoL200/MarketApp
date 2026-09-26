using MarketApp.Application.Persistence.Contracts;
using MarketApp.Infrastructure.Persistence;
using MarketApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped(
            typeof(IGeneralRepository<>),
            typeof(GeneralRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}