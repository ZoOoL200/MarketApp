using System.Security.Claims;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Accounting;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MarketApp.Api.Controllers;

[ApiController, Route("api/stakeholder-expenses")]
[Authorize(Policy = "AuthenticatedUser", Roles = AppRoles.Stakeholder)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StakeholderExpensesController(IStakeholderExpenseService service, IStakeholderExpenseQueries queries) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost, ProducesResponseType(typeof(StakeholderExpenseDto), 200)]
    public async Task<IActionResult> Create(CreateStakeholderExpenseDto request, CancellationToken ct)
        => Ok(await service.CreateAsync(Actor, request, ct));
    [HttpGet, ProducesResponseType(typeof(StakeholderExpensePageDto), 200)]
    public async Task<IActionResult> List([FromQuery] StakeholderExpenseQuery query, CancellationToken ct)
        => Ok(await queries.ListAsync(query, ct));
    [HttpGet("{expenseId:guid}"), ProducesResponseType(typeof(StakeholderExpenseDto), 200)]
    public async Task<IActionResult> Get(Guid expenseId, CancellationToken ct)
        => Ok(await queries.GetAsync(expenseId, ct));
    [HttpPost("{expenseId:guid}/void"), ProducesResponseType(typeof(StakeholderExpenseDto), 200)]
    public async Task<IActionResult> Void(Guid expenseId, VoidStakeholderExpenseDto request, CancellationToken ct)
        => Ok(await service.VoidAsync(expenseId, Actor, request, ct));
}
