using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MarketApp.Api.Controllers;
using MarketApp.Api.ExceptionHandling;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Application.Services;
using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Entity.Sales;
using MarketApp.Infrastructure.Identity;
using MarketApp.Infrastructure.Persistence;
using MarketApp.Infrastructure.Repositories;
using MarketApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MarketApp.Api.Tests;

public sealed class SalesApiTests : IDisposable
{
    private readonly SqliteConnection _sqlite = new("Data Source=:memory:");
    private readonly string? _postgres;
    private readonly string _schema = "market_test_" + Guid.NewGuid().ToString("N");
    private readonly IHost _host;
    private readonly HttpClient _client;
    private readonly Guid _branch = Guid.NewGuid(), _otherBranch = Guid.NewGuid(), _location = Guid.NewGuid();
    private readonly Guid _seller = Guid.NewGuid(), _manager = Guid.NewGuid(), _owner = Guid.NewGuid(), _otherSeller = Guid.NewGuid();
    private readonly Guid _product = Guid.NewGuid();
    private string Base => $"/api/branches/{_branch}";
    public SalesApiTests()
    {
        var postgres = Environment.GetEnvironmentVariable("MARKETAPP_TEST_POSTGRES");
        if (!string.IsNullOrWhiteSpace(postgres))
        {
            var builder = new NpgsqlConnectionStringBuilder(postgres) { SearchPath = _schema, Pooling = false };
            _postgres = builder.ConnectionString;
            using var connection = new NpgsqlConnection(_postgres); connection.Open();
            using var command = new NpgsqlCommand($"CREATE SCHEMA {_schema}", connection); command.ExecuteNonQuery();
        }
        else _sqlite.Open();
        using (var db = NewDb())
        {
            if (_postgres is null) db.Database.EnsureCreated(); else db.GetService<IRelationalDatabaseCreator>().CreateTables();
            Seed(db);
        }
        _host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddLogging(); services.AddControllers().AddApplicationPart(typeof(SalesController).Assembly);
            services.AddProblemDetails(); services.AddExceptionHandler<DatabaseConflictHandler>(); services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddDataProtection();
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 4; options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false; options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager().AddDefaultTokenProviders();
            var jwt = new JwtSettings { Issuer = "MarketApp.Tests", Audience = "MarketApp.Tests", SigningKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) };
            services.AddSingleton(jwt);
            services.AddAuthentication("Smart").AddPolicyScheme("Smart", "Test or JWT", options =>
                options.ForwardDefaultSelector = context => context.Request.Headers.ContainsKey("Authorization") ? "Bearer" : "Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { })
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true, IssuerSigningKey = jwt.CreateSigningKey(), ValidateLifetime = true,
                        NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role, ClockSkew = TimeSpan.Zero
                    };
                });
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IAuthSessionService, AuthSessionService>();
            services.AddScoped<IAccessTokenService, JwtAccessTokenService>();
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddSingleton<IApplicationEmailSender, TestEmail>();
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(AppRoles.Stakeholder).Build();
                options.FallbackPolicy = options.DefaultPolicy;
                options.AddPolicy("AuthenticatedUser", p => p.RequireAuthenticatedUser());
            });
            services.AddScoped<AppDbContext>(_ => NewDb());
            services.AddScoped(typeof(IGeneralRepository<>), typeof(GeneralRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISalesService, SalesService>();
            services.AddScoped<ISalesQueries, SalesQueries>();
            services.AddScoped<IBranchAccessService, BranchAccessService>();
        }).Configure(app =>
        {
            app.UseExceptionHandler(); app.UseRouting(); app.UseAuthentication();
            app.UseWhen(context => context.Request.Headers.ContainsKey("Authorization"), branch => branch.UseMiddleware<IdentitySessionMiddleware>());
            app.UseAuthorization();
            app.UseEndpoints(e => { e.MapControllers(); });
        })).Start();
        _client = _host.GetTestClient(); Role(AppRoles.Seller, _seller);
    }
    private AppDbContext NewDb() => _postgres is null
        ? new TestDb(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_sqlite).Options)
        : new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres).Options);
    private void Seed(AppDbContext db)
    {
        db.Branches.AddRange(new Branch { Id = _branch, Name = "Main", Code = "B1" }, new Branch { Id = _otherBranch, Name = "Other", Code = "B2" });
        db.InventoryLocations.Add(new() { Id = _location, BranchId = _branch, Name = "Shop", Code = "L1" });
        var category = new Category { Name = "Test" }; db.Categories.Add(category);
        db.Products.Add(new() { Id = _product, CategoryId = category.Id, Name = "Product", Sku = "SKU-1" });
        foreach (var id in new[] { _seller, _manager, _owner, _otherSeller })
            db.Users.Add(new() { Id = id, UserName = id.ToString(), NormalizedUserName = id.ToString().ToUpperInvariant(), Email = id + "@example.com",
                NormalizedEmail = id + "@EXAMPLE.COM", FullName = "Test User", IsActive = true, EmailConfirmed = true, SecurityStamp = Guid.NewGuid().ToString(), PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), "pass1234") });
        var sellerRole = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = AppRoles.Seller, NormalizedName = "SELLER" };
        var managerRole = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = AppRoles.BranchManager, NormalizedName = "BRANCHMANAGER" };
        db.Roles.AddRange(sellerRole, managerRole);
        db.UserRoles.AddRange(new() { UserId = _seller, RoleId = sellerRole.Id }, new() { UserId = _manager, RoleId = managerRole.Id });
        db.UserBranchAssignments.AddRange(new() { UserId = _seller, BranchId = _branch }, new() { UserId = _otherSeller, BranchId = _branch }, new() { UserId = _manager, BranchId = _branch });
        db.StockBalances.Add(new() { ProductId = _product, InventoryLocationId = _location, Quantity = 10 });
        var price = new BranchProductPrice { BranchId = _branch, ProductId = _product, BaselineUnitPrice = 100,
            MinimumSellingPrice = 150, Revision = 1, BaselineRevision = 1 };
        db.BranchProductPrices.Add(price);
        db.BranchProductPriceHistories.Add(new() { BranchProductPriceId = price.Id, Revision = 1, NewBaselineUnitPrice = 100,
            NewMinimumSellingPrice = 150, ChangedAtUtc = DateTime.UtcNow.AddDays(-1), Reason = "Test seed", ChangeType = MarketApp.Domain.Enums.BranchPriceChangeType.ManualBaseline });
        db.SaveChanges();
    }
    private void Role(string role, Guid id)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-Role"); _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-Role", role); _client.DefaultRequestHeaders.Add("X-Test-User", id.ToString());
    }
    private CreateSaleDto Request(decimal quantity = 2, decimal price = 170) => new()
    {
        ClientSaleId = Guid.NewGuid(), InventoryLocationId = _location,
        Lines = [new() { ProductId = _product, Quantity = quantity, SellingUnitPrice = price, PriceRevision = 1 }]
    };
    private async Task<SaleDto> Post(CreateSaleDto? request = null)
    {
        var response = await _client.PostAsJsonAsync(Base + "/sales", request ?? Request());
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SaleDto>())!;
    }
    private async Task<decimal> Stock() { await using var db = NewDb(); return (await db.StockBalances.SingleAsync(b => b.ProductId == _product)).Quantity; }
    [Fact]
    public async Task Sale_is_atomic_idempotent_and_keeps_historical_prices()
    {
        var request = Request(); var sale = await Post(request);
        Assert.Equal(340, sale.Total); Assert.Equal(8, await Stock());
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
        Assert.Equal(8, await Stock());
        request.Lines[0].Quantity = 1;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
        using var db = NewDb();
        Assert.Single(await db.SalesInvoices.ToListAsync());
        var movement = Assert.Single(await db.StockMovements.ToListAsync());
        Assert.Equal(-2, movement.QuantityChange); Assert.Equal(sale.Lines[0].Id, movement.SalesInvoiceLineId);
        await db.BranchProductPrices.ExecuteUpdateAsync(s => s.SetProperty(p => p.BaselineUnitPrice, 120));
        Assert.Equal(100, (await db.SalesInvoiceLines.SingleAsync()).BaselineUnitPriceSnapshot);
        var json = await _client.GetStringAsync(Base + "/sales/" + sale.Id);
        Assert.DoesNotContain("baseline", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("share", json, StringComparison.OrdinalIgnoreCase);
    }
    [Theory]
    [InlineData(0, 170, HttpStatusCode.BadRequest)]
    [InlineData(-1, 170, HttpStatusCode.BadRequest)]
    [InlineData(1.0001, 170, HttpStatusCode.BadRequest)]
    [InlineData(1, 149, HttpStatusCode.Conflict)]
    [InlineData(11, 170, HttpStatusCode.Conflict)]
    public async Task Rejects_bad_sale_without_changing_stock(decimal quantity, decimal price, HttpStatusCode status)
    {
        Assert.Equal(status, (await _client.PostAsJsonAsync(Base + "/sales", Request(quantity, price))).StatusCode);
        Assert.Equal(10, await Stock());
        using var db = NewDb(); Assert.Empty(await db.SalesInvoices.ToListAsync()); Assert.Empty(await db.StockMovements.ToListAsync());
    }
    [Fact]
    public async Task Failure_on_later_line_rolls_back_the_entire_sale()
    {
        var request = Request();
        request.Lines.Add(new() { ProductId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Quantity = 1, SellingUnitPrice = 170, PriceRevision = 1 });
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
        Assert.Equal(10, await Stock());
        using var db = NewDb(); Assert.Empty(await db.SalesInvoices.ToListAsync());
    }
    [Fact]
    public async Task Can_sell_exactly_all_available_stock()
    {
        await Post(Request(10)); Assert.Equal(0, await Stock());
    }
    [Fact]
    public async Task Seller_is_isolated_from_other_branches_users_and_management_reports()
    {
        var sale = await Post();
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync($"/api/branches/{_otherBranch}/catalog")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/products", new { })).StatusCode);
        Role(AppRoles.Seller, _otherSeller);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(Base + "/sales/" + sale.Id)).StatusCode);
        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(Base + "/catalog")).StatusCode);
    }
    [Fact]
    public async Task Seller_catalog_and_stock_do_not_expose_baselines()
    {
        var body = await _client.GetStringAsync(Base + "/catalog?search=SKU-1");
        Assert.Contains("SKU-1", body); Assert.DoesNotContain("baseline", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(Base + "/catalog/stock")).StatusCode);
    }
    private async Task<AuthResponseDto> Login()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { userName = _seller.ToString(), password = "pass1234" });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }
    [Fact]
    public async Task Real_JWT_login_authorizes_assigned_branch_and_rejects_other_branch()
    {
        var auth = await Login();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(Base + "/catalog")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync($"/api/branches/{_otherBranch}/catalog")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/products")).StatusCode);
    }

    [PostgresFact]
    public async Task Real_JWT_login_and_logout_enforce_session_revocation()
    {
        var auth = await Login();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(Base + "/catalog")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(Base + "/catalog")).StatusCode);
    }
    [PostgresFact]
    public async Task Refresh_token_reuse_revokes_the_session()
    {
        var auth = await Login();
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var replacement = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Assert.NotEqual(auth.RefreshToken, replacement.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken })).StatusCode);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", replacement.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(Base + "/catalog")).StatusCode);
    }
    private sealed class TestEmail : IApplicationEmailSender
    {
        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [PostgresFact]
    public async Task Concurrent_sales_cannot_oversell()
    {
        var responses = await Task.WhenAll(_client.PostAsJsonAsync(Base + "/sales", Request(7)), _client.PostAsJsonAsync(Base + "/sales", Request(7)));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(3, await Stock());
    }
    [Fact]
    public async Task Stale_price_revision_is_rejected_and_current_revision_can_sell()
    {
        var request = Request();
        using (var db = NewDb())
        {
            var price = await db.BranchProductPrices.SingleAsync();
            price.Revision = 2; price.BaselineUnitPrice = 120; price.MinimumSellingPrice = 180;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
        Assert.Equal(10, await Stock());
        request.Lines[0].PriceRevision = 2; request.Lines[0].SellingUnitPrice = 190;
        await Post(request);
        using var check = NewDb();
        var line = await check.SalesInvoiceLines.SingleAsync();
        Assert.Equal(120, line.BaselineUnitPriceSnapshot);
        Assert.Equal(180, line.MinimumSellingPriceSnapshot);
        Assert.Equal(190, line.SellingUnitPrice);
    }

    [Fact]
    public async Task History_is_paginated_and_managers_can_see_branch_sales()
    {
        var first = await Post(); await Post();
        Role(AppRoles.Seller, _otherSeller);
        var page = await _client.GetFromJsonAsync<MarketApp.Application.Common.PagedResult<SaleDto>>(Base + "/sales");
        Assert.Empty(page!.Items);
        Role(AppRoles.BranchManager, _manager);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(Base + "/sales/" + first.Id)).StatusCode);
        page = await _client.GetFromJsonAsync<MarketApp.Application.Common.PagedResult<SaleDto>>(Base + "/sales?pageSize=1");
        Assert.Equal(2, page!.TotalCount); Assert.Single(page.Items);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync($"/api/branches/{_otherBranch}/sales")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync(Base + "/sales?pageSize=101")).StatusCode);
    }

    [Fact]
    public async Task Inactive_or_wrong_branch_stock_location_cannot_be_sold()
    {
        using (var db = NewDb())
        {
            var location = await db.InventoryLocations.SingleAsync(); location.BranchId = _otherBranch;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", Request())).StatusCode);
        Assert.Equal(10, await Stock());
        using (var db = NewDb())
        {
            var location = await db.InventoryLocations.SingleAsync(); location.BranchId = _branch; location.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", Request())).StatusCode);
    }

    [Fact]
    public async Task Malformed_lines_and_empty_identifiers_do_not_post()
    {
        var request = Request(); request.Lines.Add(request.Lines[0]);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
        request = Request(); request.Lines = [null!];
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
        request = Request(); request.ClientSaleId = Guid.Empty;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
        request = Request(); request.Lines = null!;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
        Assert.Equal(10, await Stock());
    }

    [Fact]
    public async Task Successful_retry_still_returns_original_invoice_after_price_changes()
    {
        var request = Request(); var sale = await Post(request);
        using (var db = NewDb())
        {
            var price = await db.BranchProductPrices.SingleAsync(); price.Revision++; price.MinimumSellingPrice = 200;
            var product = await db.Products.SingleAsync(); product.Name = "Renamed";
            await db.SaveChangesAsync();
        }
        var retry = await _client.PostAsJsonAsync(Base + "/sales", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var repeated = (await retry.Content.ReadFromJsonAsync<SaleDto>())!;
        Assert.Equal(sale.Id, repeated.Id); Assert.Equal("Product", repeated.Lines[0].ProductName);
        Assert.Equal(8, await Stock());
        Role(AppRoles.Seller, _otherSeller);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", request)).StatusCode);
    }

    [Fact]
    public async Task Missing_minimum_or_inactive_product_prevents_a_sale()
    {
        using (var db = NewDb())
        {
            var price = await db.BranchProductPrices.SingleAsync(); price.MinimumSellingPrice = null;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", Request())).StatusCode);
        using (var db = NewDb())
        {
            var price = await db.BranchProductPrices.SingleAsync(); price.MinimumSellingPrice = 150;
            var product = await db.Products.SingleAsync(); product.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", Request())).StatusCode);
        Assert.Equal(10, await Stock());
    }

    [Fact]
    public async Task Real_JWT_can_post_sale_and_cannot_read_management_prices()
    {
        var auth = await Login();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await Post();
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(Base + $"/product-prices/{_product}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(Base + "/catalog/locations")).StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose(); _host.Dispose(); _sqlite.Dispose();
        if (_postgres is not null)
        {
            using var connection = new NpgsqlConnection(_postgres); connection.Open();
            using var command = new NpgsqlCommand($"DROP SCHEMA {_schema} CASCADE", connection); command.ExecuteNonQuery();
        }
    }
    private sealed class TestDb(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);
            foreach (var entity in model.Model.GetEntityTypes())
            {
                foreach (var c in entity.GetCheckConstraints().ToArray()) entity.RemoveCheckConstraint(c.Name!);
                foreach (var p in entity.GetProperties().Where(p => p.ClrType == typeof(uint)))
                {
                    p.ValueGenerated = ValueGenerated.Never; p.IsConcurrencyToken = false;
                }
            }
        }
    }
    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Test-Role"].ToString(); var user = Request.Headers["X-Test-User"].ToString();
            if (role.Length == 0) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user), new Claim(ClaimTypes.Role, role) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MARKETAPP_TEST_POSTGRES")))
            Skip = "Set MARKETAPP_TEST_POSTGRES to a disposable PostgreSQL test database to verify real concurrent transactions.";
    }
}
