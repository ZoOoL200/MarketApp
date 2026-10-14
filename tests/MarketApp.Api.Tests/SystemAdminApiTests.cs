using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Application.DTOs.Users;
using MarketApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketApp.Api.Tests;

public sealed partial class SalesApiTests
{
    private const string AdminEndpoint = "/api/admin/stakeholders";

    // These are disposable test credentials, not real account credentials.
    private const string TestAdminPassword = "AdminTest123!";

    private static CreateStakeholderRequestDto StakeholderRequest() => new()
    {
        UserName = "createdowner",
        FullName = "Created Owner",
        Email = "createdowner@example.com",
        Password = "OwnerTest123!"
    };

    private async Task<Guid> CreateTestAdministratorAsync()
    {
        using var scope = _host.Services.CreateScope();

        var roles = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in new[]
        {
            AppRoles.SystemAdmin,
            AppRoles.Stakeholder
        })
        {
            if (await roles.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roles.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName
            });

            Assert.True(
                result.Succeeded,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var users = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var administrator = new ApplicationUser
        {
            UserName = "testadmin",
            FullName = "Test Administrator",
            Email = "testadmin@example.com",
            IsActive = true,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var created = await users.CreateAsync(
            administrator,
            TestAdminPassword);

        Assert.True(
            created.Succeeded,
            string.Join("; ", created.Errors.Select(e => e.Description)));

        var assigned = await users.AddToRoleAsync(
            administrator,
            AppRoles.SystemAdmin);

        Assert.True(
            assigned.Succeeded,
            string.Join("; ", assigned.Errors.Select(e => e.Description)));

        return administrator.Id;
    }

    private async Task LoginAsTestAdministratorAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        _client.DefaultRequestHeaders.Remove("X-Test-User");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                UserName = "testadmin",
                Password = TestAdminPassword
            });

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());

        var tokens = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(tokens);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    [Fact]
    public async Task SystemAdmin_creates_stakeholder_and_records_creator()
    {
        var administratorId = await CreateTestAdministratorAsync();
        await LoginAsTestAdministratorAsync();

        var response = await _client.PostAsJsonAsync(
            AdminEndpoint,
            StakeholderRequest());

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());

        var user = await response.Content.ReadFromJsonAsync<UserDetailsDto>();

        Assert.NotNull(user);
        Assert.Equal(AppRoles.Stakeholder, Assert.Single(user.Roles));
        Assert.Empty(user.BranchIds);
        Assert.True(user.IsActive);
        Assert.False(user.EmailConfirmed);

        await using var db = NewDb();

        var saved = await db.Users
            .AsNoTracking()
            .SingleAsync(u => u.Id == user.Id);

        Assert.Equal((Guid?)administratorId, saved.CreatedByUserId);
        Assert.NotEqual(default(DateTime), saved.CreatedAtUtc);

        var savedRoles = await (
            from assignment in db.UserRoles
            join role in db.Roles on assignment.RoleId equals role.Id
            where assignment.UserId == user.Id
            select role.Name
        ).ToListAsync();

        Assert.Equal(AppRoles.Stakeholder, Assert.Single(savedRoles));

        Assert.False(await db.UserBranchAssignments
            .AnyAsync(a => a.UserId == user.Id));
    }

    [Theory]
    [InlineData(AppRoles.Stakeholder)]
    [InlineData(AppRoles.BranchManager)]
    [InlineData(AppRoles.Seller)]
    public async Task Other_roles_cannot_create_stakeholders(string role)
    {
        Role(role, _owner);

        await using var beforeDb = NewDb();
        var beforeCount = await beforeDb.Users.CountAsync();

        var response = await _client.PostAsJsonAsync(
            AdminEndpoint,
            StakeholderRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var afterDb = NewDb();
        Assert.Equal(beforeCount, await afterDb.Users.CountAsync());
    }

    [Fact]
    public async Task Anonymous_user_cannot_create_stakeholder()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        _client.DefaultRequestHeaders.Remove("X-Test-User");

        var response = await _client.PostAsJsonAsync(
            AdminEndpoint,
            StakeholderRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_stakeholder_username_and_email_are_rejected()
    {
        await CreateTestAdministratorAsync();
        await LoginAsTestAdministratorAsync();

        var first = await _client.PostAsJsonAsync(
            AdminEndpoint,
            StakeholderRequest());

        Assert.True(
            first.StatusCode == HttpStatusCode.Created,
            await first.Content.ReadAsStringAsync());

        await using var beforeDb = NewDb();
        var beforeCount = await beforeDb.Users.CountAsync();

        var duplicateName = StakeholderRequest();
        duplicateName.Email = "different@example.com";

        var nameResponse = await _client.PostAsJsonAsync(
            AdminEndpoint,
            duplicateName);

        Assert.Equal(HttpStatusCode.Conflict, nameResponse.StatusCode);

        var duplicateEmail = StakeholderRequest();
        duplicateEmail.UserName = "differentowner";

        var emailResponse = await _client.PostAsJsonAsync(
            AdminEndpoint,
            duplicateEmail);

        Assert.Equal(HttpStatusCode.Conflict, emailResponse.StatusCode);

        await using var afterDb = NewDb();
        Assert.Equal(beforeCount, await afterDb.Users.CountAsync());
    }

    [Fact]
    public async Task SystemAdmin_cannot_access_business_management_routes()
    {
        await CreateTestAdministratorAsync();
        await LoginAsTestAdministratorAsync();

        foreach (var route in new[]
        {
            "/api/products",
            "/api/purchase-invoices",
            "/api/users"
        })
        {
            var response = await _client.GetAsync(route);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Stakeholder_cannot_disable_SystemAdmin_through_staff_endpoint()
    {
        var administratorId = await CreateTestAdministratorAsync();

        Role(AppRoles.Stakeholder, _owner);

        var response = await _client.PutAsJsonAsync(
            $"/api/users/{administratorId}/access",
            new UpdateUserAccessDto
            {
                IsActive = false
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = NewDb();

        var administrator = await db.Users
            .AsNoTracking()
            .SingleAsync(u => u.Id == administratorId);

        Assert.True(administrator.IsActive);

        Assert.False(await db.UserBranchAssignments
            .AnyAsync(a => a.UserId == administratorId));
    }

    [Fact]
    public async Task SystemAdmin_initialization_creates_all_roles_and_is_repeatable()
    {
        // Remove roles only from this test's isolated database.
        // This simulates initialization with no existing role records.
        await using (var db = NewDb())
        {
            await db.UserRoles.ExecuteDeleteAsync();
            await db.Roles.ExecuteDeleteAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapSystemAdmin:UserName"] = "bootstrapadmin",
                ["BootstrapSystemAdmin:FullName"] = "Bootstrap Administrator",
                ["BootstrapSystemAdmin:Email"] = "bootstrap@example.com",
                ["BootstrapSystemAdmin:Password"] = "InitialTest123!"
            })
            .Build();

        using (var scope = _host.Services.CreateScope())
        {
            var initializer = new IdentityInitializer(
                scope.ServiceProvider.GetRequiredService<
                    MarketApp.Infrastructure.Persistence.AppDbContext>(),
                scope.ServiceProvider.GetRequiredService<
                    UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<
                    RoleManager<IdentityRole<Guid>>>(),
                configuration);

            await initializer.InitializeSystemAdminAsync();
        }

        // A second run must not replace the existing password.
        configuration["BootstrapSystemAdmin:Password"] = "ChangedTest456!";

        using (var scope = _host.Services.CreateScope())
        {
            var users = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var roles = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            var initializer = new IdentityInitializer(
                scope.ServiceProvider.GetRequiredService<
                    MarketApp.Infrastructure.Persistence.AppDbContext>(),
                users,
                roles,
                configuration);

            await initializer.InitializeSystemAdminAsync();

            foreach (var roleName in new[]
            {
                AppRoles.SystemAdmin,
                AppRoles.Stakeholder,
                AppRoles.BranchManager,
                AppRoles.Seller
            })
            {
                Assert.True(await roles.RoleExistsAsync(roleName));
            }

            var administrators =
                await users.GetUsersInRoleAsync(AppRoles.SystemAdmin);

            var administrator = Assert.Single(administrators);

            Assert.Equal("bootstrapadmin", administrator.UserName);

            Assert.Equal(
                AppRoles.SystemAdmin,
                Assert.Single(await users.GetRolesAsync(administrator)));

            Assert.True(await users.CheckPasswordAsync(
                administrator,
                "InitialTest123!"));

            Assert.False(await users.CheckPasswordAsync(
                administrator,
                "ChangedTest456!"));
        }
    }
}