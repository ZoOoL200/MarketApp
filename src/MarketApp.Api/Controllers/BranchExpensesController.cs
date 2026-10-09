using System.Security.Claims;
using MarketApp.Application.Common;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MarketApp.Api.Controllers;
[ApiController, Route("api/branches/{branchId:guid}/expenses")]
[Authorize(Policy = "AuthenticatedUser", Roles = AppRoles.Stakeholder + "," + AppRoles.BranchManager)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BranchExpensesController(IAccountingService service, IAccountingQueries queries, IBranchAccessService access) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet, ProducesResponseType(typeof(PagedResult<ExpenseDto>), 200)]
    public async Task<IActionResult> List(Guid branchId, [FromQuery] PageQuery page, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await queries.ExpensesAsync(branchId, page, ct));
    }
    [HttpPost, ProducesResponseType(typeof(ExpenseDto), 200)]
    public async Task<IActionResult> Create(Guid branchId, CreateExpenseDto request, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await service.CreateExpenseAsync(branchId, Actor, request, ct));
    }
    [HttpPost("{expenseId:guid}/void"), ProducesResponseType(typeof(ExpenseDto), 200)]
    public async Task<IActionResult> Void(Guid branchId, Guid expenseId, VoidExpenseDto request, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await service.VoidExpenseAsync(branchId, expenseId, Actor, request, ct));
    }
}
