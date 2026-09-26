using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketApp.Application.Extensions;

public static class ApplicationServicesRegistration
{
    public static IServiceCollection ApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IInventoryLocationService,InventoryLocationService>();
        services.AddScoped<IPurchaseInvoiceService,PurchaseInvoiceService>();
        services.AddScoped<IStockService, StockService>();

        return services;
    }
}