using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using MarketApp.Application.Common;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace MarketApp.Api.Tests;

public sealed partial class SalesApiTests
{
    private CreateReturnDto ReturnRequest(SaleDto sale, decimal quantity = 1, bool restock = true) => new()
    {
        ClientReturnId = Guid.NewGuid(), Reason = "Customer returned item",
        Lines = [new() { SalesInvoiceLineId = sale.Lines[0].Id, Quantity = quantity, Restock = restock }]
    };
    private Task<HttpResponseMessage> ReturnSale(SaleDto sale, CreateReturnDto request)
        => _client.PostAsJsonAsync(Base + $"/sales/{sale.Id}/returns", request);
    private string ReportUrl(DateTime from, DateTime to) => Base + $"/reports/profit?fromUtc={Uri.EscapeDataString(from.ToString("O"))}&toUtc={Uri.EscapeDataString(to.ToString("O"))}";
    private async Task<BranchReportDto> Report(DateTime? from = null, DateTime? to = null)
    {
        var response = await _client.GetAsync(ReportUrl(from ?? DateTime.UtcNow.AddDays(-2), to ?? DateTime.UtcNow.AddDays(1)));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<BranchReportDto>())!;
    }
    private CreateAdjustmentDto Adjustment(decimal delta = 3) => new()
    {
        ClientAdjustmentId = Guid.NewGuid(), ProductId = _product, InventoryLocationId = _location,
        QuantityChange = delta, Reason = "Physical stock count correction"
    };
    private SyncSaleDto Offline(decimal quantity = 2, decimal price = 170) => new()
        { SoldAtUtc = DateTime.UtcNow.AddMinutes(-10), Sale = Request(quantity, price) };
    private async Task NewPrice(DateTime effectiveAt)
    {
        await using var db = NewDb(); var price = await db.BranchProductPrices.SingleAsync();
        price.Revision = 2; price.BaselineUnitPrice = 200; price.MinimumSellingPrice = 250;
        db.BranchProductPriceHistories.Add(new BranchProductPriceHistory { BranchProductPriceId = price.Id,
            Revision = 2, NewBaselineUnitPrice = 200, NewMinimumSellingPrice = 250, ChangedAtUtc = effectiveAt,
            Reason = "Inflation", ChangeType = BranchPriceChangeType.ManualBaseline });
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(true, 9, 100, 70)]
    public async Task Returns_preserve_original_prices_and_handle_restock(bool restock, decimal stock, decimal baseline, decimal profit)
    {
        var sale = await Post(); await NewPrice(DateTime.UtcNow);
        Role(AppRoles.BranchManager, _manager);
        var request = ReturnRequest(sale, restock: restock);
        var response = await ReturnSale(sale, request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var result = (await response.Content.ReadFromJsonAsync<ReturnDto>())!;
        Assert.Equal(170, result.Refund); Assert.Equal(request.ClientReturnId, result.ClientReturnId);
        Assert.Equal(_manager, result.CreatedByUserId); Assert.Equal(stock, await Stock());
        Assert.Equal(result.Id, (await (await ReturnSale(sale, request)).Content.ReadFromJsonAsync<ReturnDto>())!.Id);
        Assert.Equal(stock, await Stock());
        var report = await Report(); Assert.Equal(340, report.SalesRevenue); Assert.Equal(170, report.Refunds);
        Assert.Equal(170, report.NetRevenue); Assert.Equal(baseline, report.NetBaselineValue); Assert.Equal(profit, report.GrossProfit);
        await using var db = NewDb(); Assert.Equal(1, await db.SalesReturns.CountAsync());
        Assert.Equal(restock ? 1 : 0, await db.StockMovements.CountAsync(m => m.MovementType == StockMovementType.SalesReturn));
        Role(AppRoles.Seller, _seller);
        var body = await _client.GetStringAsync(Base + "/sales/" + sale.Id);
        Assert.DoesNotContain("baseline", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, JsonSerializer.Deserialize<SaleDto>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Lines[0].ReturnedQuantity);
    }
    [Fact]
    public async Task Return_limits_payload_reuse_and_multi_line_failure_leave_no_partial_changes()
    {
        var sale = await Post(); Role(AppRoles.BranchManager, _manager);
        var request = ReturnRequest(sale);
        Assert.Equal(HttpStatusCode.OK, (await ReturnSale(sale, request)).StatusCode);
        request.Lines[0].Quantity = 2;
        Assert.Equal(HttpStatusCode.Conflict, (await ReturnSale(sale, request)).StatusCode);
        request.ClientReturnId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await ReturnSale(sale, request)).StatusCode);
        request = ReturnRequest(sale);
        request.Lines.Add(new() { SalesInvoiceLineId = Guid.NewGuid(), Quantity = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, (await ReturnSale(sale, request)).StatusCode);
        Assert.Equal(9, await Stock());
        await using var db = NewDb(); Assert.Equal(1, await db.SalesReturns.CountAsync());
        Assert.Equal(1, (await db.SalesInvoiceLines.SingleAsync()).ReturnedQuantity);
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(0.0001)]
    public async Task Invalid_return_quantity_does_not_change_stock(decimal quantity)
    {
        var sale = await Post(); Role(AppRoles.BranchManager, _manager);
        Assert.Equal(HttpStatusCode.BadRequest, (await ReturnSale(sale, ReturnRequest(sale, quantity))).StatusCode);
        Assert.Equal(8, await Stock());
    }
    [Fact]
    public async Task Returns_reject_duplicate_or_null_lines_and_wrong_sale()
    {
        var sale = await Post(); var other = await Post(); Role(AppRoles.BranchManager, _manager);
        var request = ReturnRequest(sale); request.Lines.Add(request.Lines[0]);
        Assert.Equal(HttpStatusCode.BadRequest, (await ReturnSale(sale, request)).StatusCode);
        request.Lines = [null!]; Assert.Equal(HttpStatusCode.BadRequest, (await ReturnSale(sale, request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ReturnSale(other, ReturnRequest(sale))).StatusCode);
        Assert.Equal(6, await Stock());
    }
    [Fact]
    public async Task Batch2_permissions_isolate_sellers_and_branch_managers()
    {
        var sale = await Post();
        Assert.Equal(HttpStatusCode.Forbidden, (await ReturnSale(sale, ReturnRequest(sale))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(Base + "/sales-returns")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(ReportUrl(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/stock-adjustments", Adjustment())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/stock-adjustments")).StatusCode);
        Role(AppRoles.BranchManager, _manager);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/stock-adjustments", Adjustment())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync($"/api/branches/{_otherBranch}/sales-returns")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync($"/api/branches/{_otherBranch}/sales/{sale.Id}/returns", ReturnRequest(sale))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(ReportUrl(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow).Replace(_branch.ToString(), _otherBranch.ToString()))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync(Base + "/sales/reconcile", new ReconcileSaleDto { OriginalSellerUserId = _seller, Submission = Offline() })).StatusCode);
    }
    [Fact]
    public async Task Adjustments_are_audited_idempotent_and_cannot_make_negative_stock()
    {
        Role(AppRoles.Stakeholder, _owner); var request = Adjustment();
        var response = await _client.PostAsJsonAsync("/api/stock-adjustments", request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var result = (await response.Content.ReadFromJsonAsync<AdjustmentDto>())!;
        Assert.Equal(13, await Stock()); Assert.Equal(_owner, result.CreatedByUserId); Assert.Equal(request.ClientAdjustmentId, result.ClientAdjustmentId);
        Assert.Equal(result.Id, (await (await _client.PostAsJsonAsync("/api/stock-adjustments", request)).Content.ReadFromJsonAsync<AdjustmentDto>())!.Id);
        Assert.Equal(13, await Stock()); request.QuantityChange = 4;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/stock-adjustments", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/stock-adjustments", Adjustment(-14))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/stock-adjustments", Adjustment(-13))).StatusCode);
        Assert.Equal(0, await Stock());
        var page = await _client.GetFromJsonAsync<PagedResult<AdjustmentDto>>("/api/stock-adjustments?pageSize=1");
        Assert.Equal(2, page!.TotalCount);
        await using var db = NewDb(); Assert.Equal(2, await db.StockAdjustments.CountAsync());
        Assert.Equal(2, await db.StockMovements.CountAsync(m => m.StockAdjustmentId != null));
    }
    [Theory]
    [InlineData(0)] [InlineData(0.0001)] [InlineData(1000001)]
    public async Task Invalid_adjustment_quantity_is_rejected(decimal delta)
    {
        Role(AppRoles.Stakeholder, _owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/stock-adjustments", Adjustment(delta))).StatusCode);
        Assert.Equal(10, await Stock());
    }
    [Fact]
    public async Task Offline_sync_preserves_time_and_retry_uses_same_invoice()
    {
        var request = Offline(); var response = await _client.PostAsJsonAsync(Base + "/sales/sync", request);
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var sale = (await response.Content.ReadFromJsonAsync<SaleDto>())!;
        Assert.True(sale.IsOffline); Assert.Null(sale.ReconciledByUserId); Assert.Equal(request.SoldAtUtc, sale.SoldAtUtc);
        Assert.True(sale.CreatedAtUtc > sale.SoldAtUtc); Assert.Equal(8, await Stock());
        await NewPrice(DateTime.UtcNow);
        var retry = await _client.PostAsJsonAsync(Base + "/sales/sync", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode); Assert.Equal(sale.Id, (await retry.Content.ReadFromJsonAsync<SaleDto>())!.Id);
        request.Sale.Lines[0].Quantity = 1;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales/sync", request)).StatusCode);
        Assert.Equal(8, await Stock());
    }
    [Fact]
    public async Task Stale_offline_sale_requires_stakeholder_and_keeps_original_baseline()
    {
        var submission = Offline(); await NewPrice(DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales/sync", submission)).StatusCode);
        Assert.Equal(10, await Stock()); Role(AppRoles.Stakeholder, _owner);
        var response = await _client.PostAsJsonAsync(Base + "/sales/reconcile", new ReconcileSaleDto { OriginalSellerUserId = _seller, Submission = submission });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var sale = (await response.Content.ReadFromJsonAsync<SaleDto>())!;
        Assert.Equal(_seller, sale.SoldByUserId); Assert.Equal(_owner, sale.ReconciledByUserId); Assert.NotNull(sale.ReconciledAtUtc);
        Assert.Equal(140, (await Report()).GrossProfit);
        await using var db = NewDb(); Assert.Equal(100, (await db.SalesInvoiceLines.SingleAsync()).BaselineUnitPriceSnapshot);
        Role(AppRoles.Seller, _seller);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync(Base + "/sales/sync", submission)).StatusCode);
        Assert.Equal(8, await Stock());
    }
    [Fact]
    public async Task Reconciliation_never_bypasses_historical_price_minimum_or_stock()
    {
        await NewPrice(DateTime.UtcNow.AddMinutes(-5)); Role(AppRoles.Stakeholder, _owner);
        async Task<HttpStatusCode> Reconcile(SyncSaleDto submission) => (await _client.PostAsJsonAsync(Base + "/sales/reconcile", new ReconcileSaleDto { OriginalSellerUserId = _seller, Submission = submission })).StatusCode;
        var request = Offline(); request.SoldAtUtc = DateTime.UtcNow.AddMinutes(-1);
        Assert.Equal(HttpStatusCode.Conflict, await Reconcile(request));
        Assert.Equal(HttpStatusCode.Conflict, await Reconcile(Offline(price: 149)));
        Assert.Equal(HttpStatusCode.Conflict, await Reconcile(Offline(quantity: 11)));
        request = Offline(); request.Sale.Lines[0].PriceRevision = 3;
        Assert.Equal(HttpStatusCode.Conflict, await Reconcile(request));
        Assert.Equal(10, await Stock());
    }
    [Fact]
    public async Task Reconciliation_requires_active_assigned_original_seller()
    {
        Role(AppRoles.Stakeholder, _owner);
        var request = new ReconcileSaleDto { OriginalSellerUserId = _owner, Submission = Offline() };
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/sales/reconcile", request)).StatusCode);
        await using (var db = NewDb()) { (await db.Users.SingleAsync(u => u.Id == _seller)).IsActive = false; await db.SaveChangesAsync(); }
        request.OriginalSellerUserId = _seller;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/sales/reconcile", request)).StatusCode);
        Assert.Equal(10, await Stock());
    }
    [Theory]
    [InlineData(-31)] [InlineData(1)]
    public async Task Offline_timestamp_outside_allowed_window_is_rejected(int days)
    {
        var request = Offline(); request.SoldAtUtc = DateTime.UtcNow.AddDays(days);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/sales/sync", request)).StatusCode);
        Assert.Equal(10, await Stock());
    }
    [Fact]
    public async Task Offline_missing_UTC_or_historical_revision_time_is_rejected()
    {
        var request = Offline(); request.SoldAtUtc = DateTime.SpecifyKind(request.SoldAtUtc, DateTimeKind.Unspecified);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/sales/sync", request)).StatusCode);
        request.SoldAtUtc = DateTime.UtcNow.AddDays(-2);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales/sync", request)).StatusCode);
        Assert.Equal(10, await Stock());
    }
    [Fact]
    public async Task Batch1_persisted_hash_remains_compatible_with_online_retries()
    {
        var request = Request(); var sale = await Post(request);
        var oldHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { branchId = _branch, actorId = _seller, request })));
        await using (var db = NewDb()) { var persisted = await db.SalesInvoices.SingleAsync(); persisted.RequestHash = oldHash; await db.SaveChangesAsync(); }
        var retry = await _client.PostAsJsonAsync(Base + "/sales", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode); Assert.Equal(sale.Id, (await retry.Content.ReadFromJsonAsync<SaleDto>())!.Id);
        Assert.Equal(8, await Stock());
    }
    [Fact]
    public async Task Reports_use_sale_time_exclusive_end_and_return_posting_date()
    {
        var sale = await Post(); var boundary = DateTime.UtcNow.Date;
        await using (var db = NewDb()) { (await db.SalesInvoices.SingleAsync()).SoldAtUtc = boundary.AddDays(-1); await db.SaveChangesAsync(); }
        Role(AppRoles.BranchManager, _manager); Assert.Equal(HttpStatusCode.OK, (await ReturnSale(sale, ReturnRequest(sale))).StatusCode);
        var today = await Report(boundary, boundary.AddDays(1));
        Assert.Equal(0, today.SaleCount); Assert.Equal(170, today.Refunds); Assert.Equal(-100, today.NetBaselineValue); Assert.Equal(-70, today.GrossProfit);
        var yesterday = await Report(boundary.AddDays(-1), boundary);
        Assert.Equal(1, yesterday.SaleCount); Assert.Equal(140, yesterday.GrossProfit); Assert.Equal(0, yesterday.Refunds);
        Assert.Equal(0, (await Report(boundary.AddDays(-2), boundary.AddDays(-1))).SaleCount);
    }
    [Fact]
    public async Task Reports_validate_date_ranges_and_paging()
    {
        Role(AppRoles.BranchManager, _manager); var now = DateTime.UtcNow;
        foreach (var url in new[] { ReportUrl(now, now), ReportUrl(now, now.AddDays(32)), ReportUrl(now, now.AddDays(-1)), Base + "/sales-returns?pageSize=101" })
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync(url)).StatusCode);
    }
    [PostgresFact]
    public async Task Concurrent_returns_cannot_exceed_original_sale()
    {
        var sale = await Post(); Role(AppRoles.BranchManager, _manager);
        var responses = await Task.WhenAll(ReturnSale(sale, ReturnRequest(sale, 2)), ReturnSale(sale, ReturnRequest(sale, 2)));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(10, await Stock());
        await using var db = NewDb(); Assert.Equal(2, (await db.SalesInvoiceLines.SingleAsync()).ReturnedQuantity);
    }
    [PostgresFact]
    public async Task Concurrent_duplicate_offline_sync_deducts_stock_once()
    {
        var request = Offline();
        var responses = await Task.WhenAll(_client.PostAsJsonAsync(Base + "/sales/sync", request), _client.PostAsJsonAsync(Base + "/sales/sync", request));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync(Base + "/sales/sync", request)).StatusCode);
        Assert.Equal(8, await Stock());
    }
}
