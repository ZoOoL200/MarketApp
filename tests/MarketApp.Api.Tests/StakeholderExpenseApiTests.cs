using System.Net;
using System.Net.Http.Json;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Accounting;
using Microsoft.EntityFrameworkCore;
namespace MarketApp.Api.Tests;
public sealed partial class SalesApiTests
{
    private const string StakeholderExpensesUrl = "/api/stakeholder-expenses";
    private CreateStakeholderExpenseDto StakeholderExpense(decimal amount = 500) => new()
    {
        ClientExpenseId = Guid.NewGuid(), BranchId = _branch, Category = "Rent", Amount = amount,
        OccurredAtUtc = DateTime.UtcNow, Description = "October branch premises rent", PaidTo = "Landlord", ReferenceNumber = "RENT-001"
    };
    private async Task<StakeholderExpenseDto> PostStakeholderExpense(CreateStakeholderExpenseDto request)
    {
        var response = await _client.PostAsJsonAsync(StakeholderExpensesUrl, request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<StakeholderExpenseDto>())!;
    }
    private async Task<StakeholderExpensePageDto> StakeholderExpenseList(string query = "")
    {
        var response = await _client.GetAsync(StakeholderExpensesUrl + query);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<StakeholderExpensePageDto>())!;
    }
    [Fact]
    public async Task Stakeholder_rent_is_independent_of_both_profits_and_branch_salary_expenses()
    {
        await Post(); Role(AppRoles.BranchManager, _manager); await PostExpense(Expense(100));
        var from = DateTime.UtcNow.Date; var to = from.AddDays(1);
        var beforeBranch = await Report(from, to);
        Role(AppRoles.Stakeholder, _owner); var beforeOwner = await OwnerProfit();
        var rent = await PostStakeholderExpense(StakeholderExpense());
        Assert.Equal("Main", rent.BranchName); Assert.Equal(_owner, rent.CreatedByUserId);
        Assert.Equal(500, (await StakeholderExpenseList()).ActiveAmount);
        var branch = await Report(from, to); var owner = await OwnerProfit();
        Assert.Equal(beforeBranch, branch); Assert.Equal(140, branch.GrossProfit); Assert.Equal(40, branch.NetProfit);
        Assert.Equal(beforeOwner.GrossProfit, owner.GrossProfit); Assert.Equal(40, owner.GrossProfit);
        Assert.Equal(beforeOwner.NetPurchaseCost, owner.NetPurchaseCost);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync(StakeholderExpensesUrl + $"/{rent.Id}/void",
            new VoidStakeholderExpenseDto { Reason = "Correcting expense register" })).StatusCode);
        Assert.Equal(beforeBranch, await Report(from, to)); Assert.Equal(beforeOwner.GrossProfit, (await OwnerProfit()).GrossProfit);
        await using var db = NewDb(); Assert.Single(await db.BranchExpenses.ToListAsync()); Assert.Single(await db.StakeholderExpenses.ToListAsync());
        Assert.Equal(8, (await db.StockBalances.SingleAsync()).Quantity);
    }
    [Fact]
    public async Task Stakeholder_expense_exact_retry_and_changed_payload_preserve_one_record()
    {
        Role(AppRoles.Stakeholder, _owner); var request = StakeholderExpense();
        var saved = await PostStakeholderExpense(request); var replay = await PostStakeholderExpense(request); Assert.Equal(saved, replay);
        request.Amount = 600;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(StakeholderExpensesUrl, request)).StatusCode);
        request.Amount = 500; request.BranchId = _otherBranch;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(StakeholderExpensesUrl, request)).StatusCode);
        await using var db = NewDb(); Assert.Single(await db.StakeholderExpenses.ToListAsync());
        Assert.Equal(500, (await StakeholderExpenseList()).ActiveAmount);
    }
    [Fact]
    public async Task Stakeholder_expense_register_supports_general_and_historical_branch_expenses()
    {
        Role(AppRoles.Stakeholder, _owner); var general = StakeholderExpense(25); general.BranchId = null; general.Category = "Other";
        general.Description = "General administrative cost";
        var saved = await PostStakeholderExpense(general); Assert.Null(saved.BranchId); Assert.Null(saved.BranchName);
        await using (var db = NewDb()) { (await db.Branches.SingleAsync(b => b.Id == _branch)).IsActive = false; await db.SaveChangesAsync(); }
        var old = StakeholderExpense(); old.OccurredAtUtc = DateTime.UtcNow.AddMonths(-1);
        Assert.Equal(_branch, (await PostStakeholderExpense(old)).BranchId);
        Assert.Equal(525, (await StakeholderExpenseList()).ActiveAmount);
    }
    [Fact]
    public async Task Stakeholder_expense_filters_paging_and_date_boundaries_have_full_filtered_totals()
    {
        Role(AppRoles.Stakeholder, _owner);
        var start = DateTime.UtcNow.Date.AddDays(-2); var end = start.AddDays(1);
        var first = StakeholderExpense(100); first.OccurredAtUtc = start;
        var second = StakeholderExpense(200); second.OccurredAtUtc = start.AddHours(1); second.BranchId = _otherBranch;
        var third = StakeholderExpense(300); third.OccurredAtUtc = end;
        var general = StakeholderExpense(25); general.OccurredAtUtc = start; general.Category = "Other"; general.BranchId = null;
        general.PaidTo = "Office supplier"; general.ReferenceNumber = "REF-GENERAL"; general.Description = "General purchase";
        foreach (var request in new[] { first, second, third, general }) await PostStakeholderExpense(request);
        var q = $"?fromUtc={Uri.EscapeDataString(start.ToString("O"))}&toUtc={Uri.EscapeDataString(end.ToString("O"))}&category=Rent&pageSize=1";
        var page = await StakeholderExpenseList(q); Assert.Single(page.Items); Assert.Equal(2, page.TotalCount);
        Assert.Equal(300, page.ActiveAmount); Assert.Equal(2, page.TotalPages); Assert.True(page.HasNextPage);
        var next = await StakeholderExpenseList(q + "&pageNumber=2"); Assert.Equal(300, next.ActiveAmount); Assert.NotEqual(page.Items[0].Id, next.Items[0].Id);
        var branch = await StakeholderExpenseList(q + $"&branchId={_branch}"); Assert.Equal(100, branch.ActiveAmount); Assert.Equal(1, branch.TotalCount);
        Assert.Equal(25, (await StakeholderExpenseList("?search=REF-GENERAL")).ActiveAmount);
        Assert.Equal(25, (await StakeholderExpenseList("?search=Office")).ActiveAmount);
        Assert.Equal(25, (await StakeholderExpenseList("?search=General")).ActiveAmount);
    }
    [Theory]
    [InlineData("Seller")]
    [InlineData("BranchManager")]
    public async Task Stakeholder_expense_routes_reject_sellers_and_managers(string role)
    {
        Role(AppRoles.Stakeholder, _owner); var saved = await PostStakeholderExpense(StakeholderExpense());
        Role(role, role == AppRoles.Seller ? _seller : _manager);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(StakeholderExpensesUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(StakeholderExpensesUrl + "/" + saved.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync(StakeholderExpensesUrl, StakeholderExpense())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync(StakeholderExpensesUrl + $"/{saved.Id}/void", new VoidStakeholderExpenseDto { Reason = "Unauthorized attempt" })).StatusCode);
        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(StakeholderExpensesUrl)).StatusCode);
    }
    [Fact]
    public async Task Stakeholder_expense_void_keeps_audit_and_excludes_only_register_total()
    {
        Role(AppRoles.Stakeholder, _owner); var request = StakeholderExpense(); var saved = await PostStakeholderExpense(request);
        var url = StakeholderExpensesUrl + $"/{saved.Id}/void"; var reason = new VoidStakeholderExpenseDto { Reason = "Duplicate receipt" };
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(url, new VoidStakeholderExpenseDto { Reason = "   " })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync(url, reason)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync(url, reason)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(url, new VoidStakeholderExpenseDto { Reason = "Different reason" })).StatusCode);
        Assert.Empty((await StakeholderExpenseList()).Items);
        var audit = await StakeholderExpenseList("?includeVoided=true"); Assert.Equal(0, audit.ActiveAmount);
        var entry = Assert.Single(audit.Items); Assert.Equal(_owner, entry.VoidedByUserId); Assert.NotNull(entry.VoidedAtUtc); Assert.Equal(reason.Reason, entry.VoidReason);
        var read = await _client.GetFromJsonAsync<StakeholderExpenseDto>(StakeholderExpensesUrl + "/" + entry.Id); Assert.Equal(entry, read);
        Assert.NotNull((await PostStakeholderExpense(request)).VoidedAtUtc);
        Assert.Equal(0, (await StakeholderExpenseList()).ActiveAmount);
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(1.00001)]
    public async Task Stakeholder_expense_invalid_amount_is_rejected(decimal amount)
    {
        Role(AppRoles.Stakeholder, _owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(StakeholderExpensesUrl, StakeholderExpense(amount))).StatusCode);
        Assert.Empty((await StakeholderExpenseList()).Items);
    }
    [Fact]
    public async Task Stakeholder_expense_validates_category_branch_identifiers_and_dates()
    {
        Role(AppRoles.Stakeholder, _owner);
        var invalid = new List<CreateStakeholderExpenseDto>();
        var r = StakeholderExpense(); r.Category = "Salary"; invalid.Add(r);
        r = StakeholderExpense(); r.BranchId = Guid.Empty; invalid.Add(r);
        r = StakeholderExpense(); r.ClientExpenseId = Guid.Empty; invalid.Add(r);
        r = StakeholderExpense(); r.OccurredAtUtc = DateTime.UtcNow.AddDays(1); invalid.Add(r);
        r = StakeholderExpense(); r.OccurredAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified); invalid.Add(r);
        r = StakeholderExpense(); r.Description = "   "; invalid.Add(r);
        r = StakeholderExpense(); r.PaidTo = new string('x', 201); invalid.Add(r);
        foreach (var request in invalid) Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(StakeholderExpensesUrl, request)).StatusCode);
        r = StakeholderExpense(); r.BranchId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync(StakeholderExpensesUrl, r)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(StakeholderExpensesUrl + "/" + Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync(StakeholderExpensesUrl + $"/{Guid.NewGuid()}/void", new VoidStakeholderExpenseDto { Reason = "Missing record" })).StatusCode);
        Assert.Empty((await StakeholderExpenseList()).Items);
    }
    [Theory]
    [InlineData("?pageSize=101")]
    [InlineData("?pageNumber=0")]
    [InlineData("?category=Salary")]
    [InlineData("?fromUtc=2026-10-02T00:00:00Z&toUtc=2026-10-01T00:00:00Z")]
    public async Task Stakeholder_expense_invalid_query_is_rejected(string query)
    {
        Role(AppRoles.Stakeholder, _owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync(StakeholderExpensesUrl + query)).StatusCode);
    }
    [Fact]
    public async Task New_rent_must_not_be_recorded_as_a_branch_profit_expense()
    {
        Role(AppRoles.Stakeholder, _owner); var request = Expense(500); request.Category = "Rent"; request.EmployeeUserId = null;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Base + "/expenses", request)).StatusCode);
        await using var db = NewDb(); Assert.Empty(await db.BranchExpenses.ToListAsync());
    }
}
