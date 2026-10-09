using System.Security.Claims;
using MarketApp.Application.Common;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MarketApp.Api.Controllers;
[ApiController, Route("api"), Authorize(Policy = "AuthenticatedUser", Roles = AppRoles.Stakeholder)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountingController(IAccountingService service, IAccountingQueries queries) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("stock-costs"), ProducesResponseType(typeof(PagedResult<StockCostDto>), 200)]
    public async Task<IActionResult> Costs([FromQuery] Guid? locationId, [FromQuery] PageQuery page, CancellationToken ct)
        => Ok(await queries.CostsAsync(locationId, page, ct));
    [HttpPost("stock-costs/initialize"), ProducesResponseType(typeof(StockCostDto), 200)]
    public async Task<IActionResult> Initialize(InitializeStockCostDto request, CancellationToken ct)
        => Ok(await service.InitializeCostAsync(Actor, request, ct));
    [HttpGet("reports/stakeholder-profit"), ProducesResponseType(typeof(StakeholderProfitDto), 200)]
    public async Task<IActionResult> Profit([FromQuery] Guid? branchId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
        => Ok(await queries.StakeholderProfitAsync(branchId, fromUtc, toUtc, ct));
    [HttpGet("branches/{branchId:guid}/sales/{saleId:guid}/accounting"), ProducesResponseType(typeof(IReadOnlyList<SaleAccountingLineDto>), 200)]
    public async Task<IActionResult> Sale(Guid branchId, Guid saleId, CancellationToken ct)
        => Ok(await queries.SaleCostsAsync(branchId, saleId, ct));
    [HttpPut("branches/{branchId:guid}/sales/{saleId:guid}/lines/{lineId:guid}/purchase-cost"), ProducesResponseType(204)]
    public async Task<IActionResult> Historical(Guid branchId, Guid saleId, Guid lineId, RecordHistoricalCostDto request, CancellationToken ct)
    {
        await service.RecordHistoricalCostAsync(branchId, saleId, lineId, Actor, request, ct); return NoContent();
    }
}
