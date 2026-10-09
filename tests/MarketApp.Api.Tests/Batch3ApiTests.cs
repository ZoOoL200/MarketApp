using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MarketApp.Application.Common;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.DTOs.Users;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Application.DTOs.Purchases;
using MarketApp.Application.DTOs.StockTransfers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace MarketApp.Api.Tests;
public sealed partial class SalesApiTests
{
    [Fact]
    public async Task Staff_access_changes_revoke_JWT_and_restrict_assignments()
    {
        var token = await Login(); Role(AppRoles.Stakeholder, _owner);
        var response = await _client.PutAsJsonAsync($"/api/users/{_seller}/access", new UpdateUserAccessDto { IsActive = false });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(Base + "/catalog")).StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"/api/users/{_owner}/access", new UpdateUserAccessDto { IsActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"/api/users/{_seller}/access", new UpdateUserAccessDto { IsActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/users/{_seller}/access", new UpdateUserAccessDto { IsActive = true, BranchIds = [_otherBranch] })).StatusCode);
        Role(AppRoles.Seller, _seller); Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(Base + "/catalog")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/branches/{_otherBranch}/catalog")).StatusCode);
    }
    [Fact]
    public async Task Four_character_staff_password_and_reset_invalidate_old_session()
    {
        Role(AppRoles.Stakeholder, _owner);
        var created = await _client.PostAsJsonAsync("/api/users", new CreateUserRequestDto { UserName = "newstaff", FullName = "New Staff", Email = "new@example.com", Password = "1234", Role = AppRoles.Seller, BranchIds = [_branch] });
        Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var token = await Login(); string encoded;
        using (var scope = _host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<MarketApp.Infrastructure.Identity.ApplicationUser>>();
            var user = (await users.FindByIdAsync(_seller.ToString()))!;
            encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(await users.GeneratePasswordResetTokenAsync(user)));
        }
        var reset = new ResetPasswordRequestDto { UserId = _seller, Token = encoded, NewPassword = "5678", ConfirmPassword = "5678" };
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsJsonAsync("/api/auth/reset-password", reset)).StatusCode);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(Base + "/catalog")).StatusCode); _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/auth/reset-password", reset)).StatusCode);
    }
    [Fact]
    public async Task Management_lists_are_paged_and_stakeholder_only()
    {
        foreach (var route in new[] { "/api/users", "/api/purchase-invoices", "/api/stock-transfers" }) Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(route)).StatusCode);
        Role(AppRoles.Stakeholder, _owner);
        foreach (var route in new[] { "/api/users", "/api/purchase-invoices", "/api/stock-transfers" })
        {
            var response = await _client.GetAsync(route + "?pageSize=1"); Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync(route + "?pageSize=101")).StatusCode);
        }
    }
    [Fact]
    public async Task OpenApi_is_stakeholder_only_and_contains_new_contracts()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/openapi/v1.json")).StatusCode); Role(AppRoles.Stakeholder, _owner);
        var response = await _client.GetAsync("/openapi/v1.json"); Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var root = json.RootElement; var paths = root.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/branches/{branchId}/manager-profit-shares", out _));
        Assert.True(root.GetProperty("components").GetProperty("schemas").TryGetProperty("BranchReportDto", out _));
        Assert.Equal(0, paths.GetProperty("/api/auth/login").GetProperty("post").GetProperty("security").GetArrayLength());
        var export = Environment.GetEnvironmentVariable("MARKETAPP_OPENAPI_EXPORT");
        if (!string.IsNullOrWhiteSpace(export)) await File.WriteAllTextAsync(export, root.GetRawText());
    }
    [Fact]
    public async Task Health_CORS_and_login_rate_limits_work()
    {
        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/health/ready")).StatusCode);
        foreach (var origin in new[] { "https://manager.example.com", "https://untrusted.example.com" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, Base + "/sales"); request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST"); request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
            var response = await _client.SendAsync(request);
            Assert.Equal(origin.Contains("manager"), response.Headers.Contains("Access-Control-Allow-Origin"));
        }
        HttpResponseMessage? last = null;
        for (var n = 0; n < 31; n++) last = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { UserName = "missing", Password = "1234" });
        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }
    [Fact]
    public async Task Purchase_transfer_sale_return_and_profit_integrate()
    {
        var warehouse = Guid.NewGuid(); var supplier = Guid.NewGuid();
        await using (var db = NewDb())
        {
            db.InventoryLocations.Add(new() { Id = warehouse, Name = "Warehouse", Code = "WH" });
            db.Suppliers.Add(new() { Id = supplier, Name = "Supplier", Code = "SUP" }); await db.SaveChangesAsync();
        }
        Role(AppRoles.Stakeholder, _owner);
        var created = await _client.PostAsJsonAsync("/api/purchase-invoices", new CreatePurchaseInvoiceDto { SupplierId = supplier, InventoryLocationId = warehouse, InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow), Lines = [new() { ProductId = _product, Quantity = 5, UnitCost = 80 }] });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var purchase = (await created.Content.ReadFromJsonAsync<PurchaseInvoiceDto>())!;
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/purchase-invoices/{purchase.Id}/post", null)).StatusCode);
        var transferResponse = await _client.PostAsJsonAsync("/api/stock-transfers", new CreateStockTransferDto { SourceLocationId = warehouse, DestinationLocationId = _location, Lines = [new() { ProductId = _product, Quantity = 3, BaselineUnitPrice = 110 }] });
        Assert.True(transferResponse.IsSuccessStatusCode, await transferResponse.Content.ReadAsStringAsync());
        var transfer = (await transferResponse.Content.ReadFromJsonAsync<StockTransferDto>())!;
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/stock-transfers/{transfer.Id}/ship", null)).StatusCode);
        await using (var db = NewDb())
        {
            Assert.Equal(2, (await db.StockBalances.SingleAsync(b => b.InventoryLocationId == warehouse)).Quantity);
            Assert.Equal(10, (await db.StockBalances.SingleAsync(b => b.InventoryLocationId == _location)).Quantity);
        }
        var received = await _client.PostAsync($"/api/stock-transfers/{transfer.Id}/receive", null);
        Assert.True(received.IsSuccessStatusCode, await received.Content.ReadAsStringAsync());
        var saleRequest = Request(); await using (var db = NewDb()) saleRequest.Lines[0].PriceRevision = (await db.BranchProductPrices.SingleAsync()).Revision;
        Role(AppRoles.Seller, _seller); var sale = await Post(saleRequest); Role(AppRoles.BranchManager, _manager);
        Assert.Equal(HttpStatusCode.OK, (await ReturnSale(sale, ReturnRequest(sale))).StatusCode);
        var report = await Report(); Assert.Equal(60, report.GrossProfit); Assert.Equal(60, report.NetProfit);
        await using var finalDb = NewDb(); Assert.Equal(12, (await finalDb.StockBalances.SingleAsync(b => b.InventoryLocationId == _location)).Quantity);
    }
}
