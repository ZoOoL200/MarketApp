using MarketApp.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
namespace MarketApp.Api.Tests;
public sealed class ProductionConfigurationTests
{
    private static Dictionary<string, string?> Valid() => new()
    {
        ["AllowedHosts"] = "api.example.com", ["Email:Provider"] = "Smtp", ["Email:Host"] = "smtp.example.com",
        ["Email:From"] = "market@example.com", ["DataProtection:KeysPath"] = Path.GetFullPath("keys"),
        ["PhotoStorage:LocalRoot"] = Path.GetFullPath("photos"), ["Cors:AllowedOrigins:0"] = "https://manager.example.com"
    };
    [Theory]
    [InlineData("AllowedHosts", "*")] [InlineData("Email:Provider", "Development")] [InlineData("Email:From", "invalid")]
    [InlineData("DataProtection:KeysPath", "")] [InlineData("PhotoStorage:LocalRoot", "")]
    [InlineData("Cors:AllowedOrigins:0", "https://manager.example.com/")] [InlineData("Cors:AllowedOrigins:0", "http://manager.example.com")]
    [InlineData("ReverseProxy:KnownProxies:0", "*")]
    public void Production_rejects_incomplete_configuration(string key, string value)
    {
        var settings = Valid(); settings[key] = value;
        Assert.Throws<InvalidOperationException>(() => OperationalRegistration.Validate(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), new HostingEnvironment { EnvironmentName = "Production" }));
    }
    [Fact]
    public void Complete_production_and_existing_development_settings_validate()
    {
        OperationalRegistration.Validate(new ConfigurationBuilder().AddInMemoryCollection(Valid()).Build(), new HostingEnvironment { EnvironmentName = "Production" });
        OperationalRegistration.Validate(new ConfigurationBuilder().Build(), new HostingEnvironment { EnvironmentName = "Development" });
    }
}
