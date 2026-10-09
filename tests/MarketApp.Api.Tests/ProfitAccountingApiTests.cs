using System.Net;
using System.Net.Http.Json;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.DTOs.Purchases;
using Microsoft.EntityFrameworkCore;
namespace MarketApp.Api.Tests;
public sealed partial class SalesApiTests
{
    private async Task<StakeholderProfitDto> OwnerProfit(Guid? branch = null, DateTime? from = null, DateTime? to = null)
    {
        var url = $"/api/reports/stakeholder-profit?fromUtc={Uri.EscapeDataString((from ?? DateTime.UtcNow.AddDays(-2)).ToString("O"))}&toUtc={Uri.EscapeDataString((to ?? DateTime.UtcNow.AddDays(1)).ToString("O"))}";
        if (branch.HasValue) url += "&branchId=" + branch;
        var response = await _client.GetAsync(url);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<StakeholderProfitDto>())!;
    }
    private CreateExpenseDto Expense(decimal amount = 100) => new() {
        ClientExpenseId = Guid.NewGuid(), Amount = amount, Category = "Salary", EmployeeUserId = _seller,
        OccurredAtUtc = DateTime.UtcNow, Description = "Seller salary October 2026" };
    private async Task<ExpenseDto> PostExpense(CreateExpenseDto request)
    {
        var response = await _client.PostAsJsonAsync(Base + "/expenses", request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ExpenseDto>())!;
    }
    private async Task Purchase(decimal quantity, decimal cost)
    {
        Role(AppRoles.Stakeholder, _owner);
        var supplier = Guid.NewGuid();
        await using (var db = NewDb()) { db.Suppliers.Add(new() { Id = supplier, Code = supplier.ToString("N"), Name = "Cost test supplier" }); await db.SaveChangesAsync(); }
        var response = await _client.PostAsJsonAsync("/api/purchase-invoices", new CreatePurchaseInvoiceDto {
            SupplierId = supplier, InventoryLocationId = _location, InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = [new() { ProductId = _product, Quantity = quantity, UnitCost = cost }] });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var purchase = (await response.Content.ReadFromJsonAsync<PurchaseInvoiceDto>())!;
        response = await _client.PostAsync($"/api/purchase-invoices/{purchase.Id}/post", null);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/purchase-invoices/{purchase.Id}/post", null)).StatusCode);
    }
    [Fact]
    public async Task Both_profit_margins_are_frozen_and_salary_only_reduces_branch_net()
    {
        var sale = await Post(); Role(AppRoles.BranchManager, _manager);
        await PostExpense(Expense());
        var branch = await Report(); Assert.Equal(140, branch.GrossProfit); Assert.Equal(100, branch.ExpensesTotal); Assert.Equal(40, branch.NetProfit);
        Role(AppRoles.Stakeholder, _owner);
        var owner = await OwnerProfit(); Assert.Equal(40, owner.GrossProfit); Assert.Equal(160, owner.NetPurchaseCost); Assert.Equal(0, owner.MissingCostEntries);
        Assert.Equal(40, (await OwnerProfit(_branch)).GrossProfit); Assert.Equal(0, (await OwnerProfit(_otherBranch)).GrossProfit);
        await NewPrice(DateTime.UtcNow); await Purchase(10, 120);
        Assert.Equal(40, (await OwnerProfit()).GrossProfit); Assert.Equal(140, (await Report()).GrossProfit);
        var accounting = await _client.GetFromJsonAsync<List<SaleAccountingLineDto>>(Base + $"/sales/{sale.Id}/accounting");
        var line = Assert.Single(accounting!); Assert.Equal(80, line.PurchaseUnitCost); Assert.Equal(100, line.BaselineUnitPrice); Assert.Equal(170, line.SellingUnitPrice);
        Assert.Equal(40, line.StakeholderProfit); Assert.Equal(140, line.BranchGrossProfit);
    }
    [Fact]
    public async Task Weighted_purchase_cost_and_returns_use_original_cost_after_later_receipts()
    {
        await Purchase(10, 100); // 10 at 80 + 10 at 100 = 20 at 90.
        Role(AppRoles.Seller, _seller); var sale = await Post();
        await Purchase(2, 120); // Remaining 18 at 90 + 2 at 120 = 20 at 93.
        Role(AppRoles.BranchManager, _manager);
        var request = ReturnRequest(sale);
        Assert.Equal(HttpStatusCode.OK, (await ReturnSale(sale, request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ReturnSale(sale, request)).StatusCode);
        await using (var db = NewDb()) {
            var balance = await db.StockBalances.SingleAsync(); Assert.Equal(21, balance.Quantity);
            Assert.Equal(92.857143m, balance.AveragePurchaseUnitCost);
            Assert.Equal(90, (await db.SalesInvoiceLines.SingleAsync()).PurchaseUnitCostSnapshot);
        }
        Role(AppRoles.Stakeholder, _owner); Assert.Equal(10, (await OwnerProfit()).GrossProfit);
        Assert.Equal(70, (await Report()).GrossProfit);
        Assert.Equal(HttpStatusCode.OK, (await ReturnSale(sale, ReturnRequest(sale))).StatusCode);
        Assert.Equal(0, (await OwnerProfit()).GrossProfit); Assert.Equal(0, (await Report()).GrossProfit);
        await using var final = NewDb(); Assert.Single(await final.SalesInvoices.ToListAsync()); Assert.Equal(2, await final.SalesReturns.CountAsync());
    }
    [Fact]
    public async Task Damaged_return_is_rejected_without_stock_refund_or_profit_changes()
    {
        var sale = await Post(); Role(AppRoles.BranchManager, _manager);
        var response = await ReturnSale(sale, ReturnRequest(sale, restock: false));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("Damaged", await response.Content.ReadAsStringAsync());
        Assert.Equal(8, await Stock()); Assert.Equal(140, (await Report()).GrossProfit);
        Role(AppRoles.Stakeholder, _owner); Assert.Equal(40, (await OwnerProfit()).GrossProfit);
        await using var db = NewDb(); Assert.Empty(await db.SalesReturns.ToListAsync()); Assert.Equal(0, (await db.SalesInvoiceLines.SingleAsync()).ReturnedQuantity);
    }
    [Fact]
    public async Task Expense_retries_voids_and_branch_isolation_preserve_audit()
    {
        var request = Expense();
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync(Base + "/expenses", request)).StatusCode);
        Role(AppRoles.BranchManager, _manager);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync($"/api/branches/{_otherBranch}/expenses", request)).StatusCode);
        var expense = await PostExpense(request); Assert.Equal(expense.Id, (await PostExpense(request)).Id);
        request.Amount = 99; Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/expenses", request)).StatusCode);
        Assert.Equal(-100, (await Report()).NetProfit);
        var voidUrl = Base + $"/expenses/{expense.Id}/void"; var reason = new VoidExpenseDto { Reason = "Duplicate salary entry" };
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync(voidUrl, reason)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync(voidUrl, reason)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(voidUrl, new VoidExpenseDto { Reason = "Different reason" })).StatusCode);
        Assert.Equal(0, (await Report()).ExpensesTotal);
        await using var db = NewDb(); var saved = Assert.Single(await db.BranchExpenses.ToListAsync()); Assert.NotNull(saved.VoidedAtUtc); Assert.Equal(_manager, saved.VoidedByUserId);
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(1.00001)]
    public async Task Invalid_expense_amounts_are_rejected(decimal amount)
    {
        Role(AppRoles.BranchManager, _manager);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/expenses", Expense(amount))).StatusCode);
        await using var db = NewDb(); Assert.Empty(await db.BranchExpenses.ToListAsync());
    }
    [Fact]
    public async Task Salary_employee_must_be_assigned_seller_and_dates_are_validated()
    {
        Role(AppRoles.BranchManager, _manager); var request = Expense(); request.EmployeeUserId = _manager;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/expenses", request)).StatusCode);
        request.EmployeeUserId = null; request.OccurredAtUtc = DateTime.UtcNow.AddDays(1);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/expenses", request)).StatusCode);
        request.OccurredAtUtc = DateTime.UtcNow; request.Category = "Unknown";
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/expenses", request)).StatusCode);
        request.Category = "Salary"; await PostExpense(request);
    }
    [Fact]
    public async Task Existing_unknown_stock_needs_verified_opening_cost_and_cannot_be_repriced()
    {
        await using (var db = NewDb()) { (await db.StockBalances.SingleAsync()).AveragePurchaseUnitCost = null; await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales", Request())).StatusCode);
        var request = new InitializeStockCostDto { ClientChangeId = Guid.NewGuid(), ProductId = _product, InventoryLocationId = _location,
            ExpectedQuantity = 9, PurchaseUnitCost = 75, Reason = "Verified original purchase records" };
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/stock-costs/initialize", request)).StatusCode);
        Role(AppRoles.Stakeholder, _owner);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/stock-costs/initialize", request)).StatusCode);
        request.ExpectedQuantity = 10;
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/stock-costs/initialize", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/stock-costs/initialize", request)).StatusCode);
        request.PurchaseUnitCost = 70;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/stock-costs/initialize", request)).StatusCode);
        request.ClientChangeId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/stock-costs/initialize", request)).StatusCode);
        Role(AppRoles.Seller, _seller); await Post();
        await using var check = NewDb(); Assert.Equal(75, (await check.SalesInvoiceLines.SingleAsync()).PurchaseUnitCostSnapshot);
    }
    [Fact]
    public async Task Missing_historical_cost_is_explicit_and_can_be_filled_only_once()
    {
        var sale = await Post();
        await using (var db = NewDb()) { (await db.SalesInvoiceLines.SingleAsync()).PurchaseUnitCostSnapshot = null; await db.SaveChangesAsync(); }
        Role(AppRoles.Stakeholder, _owner); var report = await OwnerProfit(); Assert.Null(report.GrossProfit); Assert.Null(report.NetPurchaseCost); Assert.Equal(1, report.MissingCostEntries);
        var url = Base + $"/sales/{sale.Id}/lines/{sale.Lines[0].Id}/purchase-cost";
        var request = new RecordHistoricalCostDto { PurchaseUnitCost = 70, Reason = "Verified historical purchase invoice" };
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PutAsJsonAsync(url, request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PutAsJsonAsync(url, request)).StatusCode);
        Assert.Equal(60, (await OwnerProfit()).GrossProfit);
        request.PurchaseUnitCost = 90; Assert.Equal(HttpStatusCode.Conflict, (await _client.PutAsJsonAsync(url, request)).StatusCode);
        await using var check = NewDb(); var line = await check.SalesInvoiceLines.SingleAsync(); Assert.Equal(_owner, line.PurchaseCostRecordedByUserId); Assert.NotNull(line.PurchaseCostRecordedAtUtc);
        Assert.Equal(80, (await check.StockBalances.SingleAsync()).AveragePurchaseUnitCost);
    }
    [Fact]
    public async Task Cost_data_is_owner_only_and_not_leaked_in_seller_responses()
    {
        var sale = await Post();
        foreach (var role in new[] { AppRoles.Seller, AppRoles.BranchManager }) {
            Role(role, role == AppRoles.Seller ? _seller : _manager);
            foreach (var url in new[] { "/api/stock-costs", "/api/reports/stakeholder-profit", Base + $"/sales/{sale.Id}/accounting" })
                Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.PutAsJsonAsync(Base + $"/sales/{sale.Id}/lines/{sale.Lines[0].Id}/purchase-cost", new RecordHistoricalCostDto { PurchaseUnitCost = 70, Reason = "Trying change" })).StatusCode);
        }
        Role(AppRoles.Seller, _seller);
        foreach (var url in new[] { Base + "/catalog", Base + "/catalog/stock", Base + $"/sales/{sale.Id}" }) {
            var json = await _client.GetStringAsync(url); Assert.DoesNotContain("purchase", json, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("baseline", json, StringComparison.OrdinalIgnoreCase);
        }
    }
    [Fact]
    public async Task Offline_sale_uses_original_cost_and_requires_review_after_cost_changes()
    {
        var request = Offline(); await Purchase(10, 100); // Current cost 90; historical cost 80.
        Role(AppRoles.Seller, _seller);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Base + "/sales/sync", request)).StatusCode);
        Role(AppRoles.Stakeholder, _owner);
        var response = await _client.PostAsJsonAsync(Base + "/sales/reconcile", new ReconcileSaleDto { OriginalSellerUserId = _seller, Submission = request });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(40, (await OwnerProfit()).GrossProfit);
        await using var db = NewDb(); Assert.Equal(80, (await db.SalesInvoiceLines.SingleAsync()).PurchaseUnitCostSnapshot);
        Assert.Equal(91.111111m, (await db.StockBalances.SingleAsync()).AveragePurchaseUnitCost);
    }
    [Fact]
    public async Task Return_in_later_period_reverses_original_margins_and_expenses_use_their_date()
    {
        var sale = await Post(); var boundary = DateTime.UtcNow.AddHours(-1);
        await using (var db = NewDb()) { (await db.SalesInvoices.SingleAsync()).SoldAtUtc = boundary.AddHours(-1); await db.SaveChangesAsync(); }
        Role(AppRoles.BranchManager, _manager); Assert.Equal(HttpStatusCode.OK, (await ReturnSale(sale, ReturnRequest(sale))).StatusCode);
        var request = Expense(); request.OccurredAtUtc = boundary; await PostExpense(request);
        var later = await Report(boundary, DateTime.UtcNow.AddHours(1)); Assert.Equal(-70, later.GrossProfit); Assert.Equal(-170, later.NetProfit);
        var earlier = await Report(boundary.AddDays(-1), boundary); Assert.Equal(140, earlier.GrossProfit); Assert.Equal(0, earlier.ExpensesTotal);
        Role(AppRoles.Stakeholder, _owner); Assert.Equal(-20, (await OwnerProfit(from: boundary, to: DateTime.UtcNow.AddHours(1))).GrossProfit);
        Assert.Equal(40, (await OwnerProfit(from: boundary.AddDays(-1), to: boundary)).GrossProfit);
    }
    [Fact]
    public async Task Old_percentage_routes_are_retired()
    {
        Role(AppRoles.Stakeholder, _owner);
        Assert.Equal(HttpStatusCode.Gone, (await _client.GetAsync(Base + "/manager-profit-shares")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await _client.PostAsJsonAsync(Base + "/manager-profit-shares", new {})).StatusCode);
    }
    [Fact]
    public async Task Stakeholder_report_combines_and_filters_populated_branches()
    {
        await Post();
        var location = Guid.NewGuid();
        await using (var db = NewDb())
        {
            db.InventoryLocations.Add(new() { Id = location, BranchId = _otherBranch, Name = "Second shop", Code = "L2" });
            db.StockBalances.Add(new() { ProductId = _product, InventoryLocationId = location, Quantity = 5, AveragePurchaseUnitCost = 60 });
            db.BranchProductPrices.Add(new() { BranchId = _otherBranch, ProductId = _product, BaselineUnitPrice = 90, MinimumSellingPrice = 100, Revision = 1, BaselineRevision = 1 });
            await db.SaveChangesAsync();
        }
        Role(AppRoles.Stakeholder, _owner); var request = Request(3, 110); request.InventoryLocationId = location;
        var response = await _client.PostAsJsonAsync($"/api/branches/{_otherBranch}/sales", request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var all = await OwnerProfit(); Assert.Equal(130, all.GrossProfit); Assert.Equal(340, all.NetPurchaseCost);
        Assert.Equal(90, (await OwnerProfit(_otherBranch)).GrossProfit); Assert.Equal(40, (await OwnerProfit(_branch)).GrossProfit);
    }
    [Fact]
    public async Task Offline_sale_without_historical_cost_is_marked_incomplete()
    {
        await using (var db = NewDb()) { db.StockCostHistories.RemoveRange(await db.StockCostHistories.ToListAsync()); await db.SaveChangesAsync(); }
        var response = await _client.PostAsJsonAsync(Base + "/sales/sync", Offline());
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Role(AppRoles.Stakeholder, _owner); Assert.Null((await OwnerProfit()).GrossProfit);
        await using var check = NewDb(); Assert.Null((await check.SalesInvoiceLines.SingleAsync()).PurchaseUnitCostSnapshot);
    }

}
