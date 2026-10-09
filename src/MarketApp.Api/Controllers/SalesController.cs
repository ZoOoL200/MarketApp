using MarketApp.Application.Common;
using System.Security.Claims;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/branches/{branchId:guid}")]
[Authorize(Policy = "AuthenticatedUser", Roles = AppRoles.Stakeholder + "," + AppRoles.BranchManager + "," + AppRoles.Seller)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SalesController(ISalesService service, ISalesQueries queries, IBranchAccessService access) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Guid? SellerFilter => User.IsInRole(AppRoles.Stakeholder) || User.IsInRole(AppRoles.BranchManager) ? null : Actor;
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(PagedResult<CatalogProductDto>), 200)]
    public async Task<IActionResult> Catalog(Guid branchId, [FromQuery] PageQuery page, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await queries.CatalogAsync(branchId, page, ct));
    }
    [HttpGet("catalog/locations")]
    [ProducesResponseType(typeof(PagedResult<CatalogLocationDto>), 200)]
    public async Task<IActionResult> Locations(Guid branchId, [FromQuery] PageQuery page, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await queries.LocationsAsync(branchId, page, ct));
    }
    [HttpGet("catalog/stock")]
    [ProducesResponseType(typeof(PagedResult<CatalogStockDto>), 200)]
    public async Task<IActionResult> Stock(Guid branchId, [FromQuery] Guid? locationId, [FromQuery] PageQuery page, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await queries.StockAsync(branchId, locationId, page, ct));
    }
    [HttpPost("sales")]
    [ProducesResponseType(typeof(SaleDto), 201)]
    [ProducesResponseType(typeof(SaleDto), 200)]
    public async Task<IActionResult> Sell(Guid branchId, [FromBody] CreateSaleDto request, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        var result = await service.SellAsync(branchId, Actor, request, ct);
        return result.AlreadyProcessed ? Ok(result.Sale) : CreatedAtAction(nameof(Get), new { branchId, saleId = result.Sale.Id }, result.Sale);
    }
    [HttpPost("sales/sync")]
    [ProducesResponseType(typeof(SaleDto), 201), ProducesResponseType(typeof(SaleDto), 200)]
    public async Task<IActionResult> Sync(Guid branchId, SyncSaleDto submission, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        var result = await service.SyncAsync(branchId, Actor, submission, ct);
        return result.AlreadyProcessed ? Ok(result.Sale) : CreatedAtAction(nameof(Get), new { branchId, saleId = result.Sale.Id }, result.Sale);
    }
    [HttpPost("sales/reconcile"), Authorize(Roles = AppRoles.Stakeholder)]
    [ProducesResponseType(typeof(SaleDto), 201), ProducesResponseType(typeof(SaleDto), 200)]
    public async Task<IActionResult> Reconcile(Guid branchId, ReconcileSaleDto request, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        if (!await queries.IsAssignedUserAsync(branchId, request.OriginalSellerUserId, AppRoles.Seller, ct) &&
            !await queries.IsAssignedUserAsync(branchId, request.OriginalSellerUserId, AppRoles.BranchManager, ct))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Choose an active seller or manager assigned to this branch." });
        var result = await service.SyncAsync(branchId, request.OriginalSellerUserId, request.Submission, ct, Actor);
        return result.AlreadyProcessed ? Ok(result.Sale) : CreatedAtAction(nameof(Get), new { branchId, saleId = result.Sale.Id }, result.Sale);
    }
    [HttpGet("sales")]
    [ProducesResponseType(typeof(PagedResult<SaleDto>), 200)]
    public async Task<IActionResult> List(Guid branchId, [FromQuery] PageQuery page, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        return Ok(await queries.SalesAsync(branchId, SellerFilter, page, ct));
    }
    [HttpGet("sales/{saleId:guid}")]
    [ProducesResponseType(typeof(SaleDto), 200)]
    public async Task<IActionResult> Get(Guid branchId, Guid saleId, CancellationToken ct)
    {
        if (!await access.CanAccessAsync(User, branchId, ct)) return Forbid();
        var result = await queries.SaleAsync(branchId, saleId, SellerFilter, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
