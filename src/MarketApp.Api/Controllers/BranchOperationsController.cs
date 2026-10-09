using System.Security.Claims;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MarketApp.Api.Controllers;
[ApiController, Route("api/branches/{branchId:guid}")]
[Authorize(Policy = "AuthenticatedUser", Roles = AppRoles.Stakeholder + "," + AppRoles.BranchManager)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BranchOperationsController(IInventoryOperationsService service, IInventoryOperationsQueries queries, IBranchAccessService access) : ControllerBase
{
    [HttpPost("sales/{saleId:guid}/returns")]
    [ProducesResponseType(typeof(ReturnDto), 200)]
    public async Task<IActionResult> Return(Guid branchId, Guid saleId, CreateReturnDto request, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await service.ReturnAsync(branchId, saleId, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), request, ct));
    }
    [HttpGet("sales-returns")]
    [ProducesResponseType(typeof(MarketApp.Application.Common.PagedResult<ReturnDto>), 200)]
    public async Task<IActionResult> Returns(Guid branchId, [FromQuery] PageQuery page, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await queries.ReturnsAsync(branchId, page, ct));
    }
    [HttpGet("reports/profit")]
    [ProducesResponseType(typeof(BranchReportDto), 200)]
    public async Task<IActionResult> Profit(Guid branchId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await queries.ReportAsync(branchId, fromUtc, toUtc, ct));
    }
}
