using System.Security.Claims;
using MarketApp.Application.Common.Results;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Pricing;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/branches/{branchId:guid}/product-prices")]
[Authorize(
    Policy = "AuthenticatedUser",
    Roles = AppRoles.Stakeholder + "," + AppRoles.BranchManager)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BranchProductPricesController : ControllerBase
{
    private readonly IBranchProductPriceService _priceService;
    private readonly IBranchAccessService _branchAccessService;

    public BranchProductPricesController(
        IBranchProductPriceService priceService,
        IBranchAccessService branchAccessService)
    {
        _priceService = priceService;
        _branchAccessService = branchAccessService;
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Get(
        Guid branchId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _branchAccessService.CanAccessAsync(
                User, branchId, cancellationToken))
        {
            return Forbid();
        }

        var price = await _priceService.GetManagementPriceAsync(
            branchId,
            productId,
            cancellationToken);

        if (price is null)
        {
            return NotFound();
        }

        return Ok(price);
    }

    [Authorize(Roles = AppRoles.Stakeholder)]
    [HttpPut("{productId:guid}/baseline")]
    public async Task<IActionResult> SetBaseline(
        Guid branchId,
        Guid productId,
        [FromBody] UpdateBranchPriceDto request,
        CancellationToken cancellationToken)
    {
        if (!await _branchAccessService.CanAccessAsync(
                User, branchId, cancellationToken))
        {
            return Forbid();
        }

        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await _priceService.SetBaselineAsync(
            branchId,
            productId,
            request,
            actorUserId,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{productId:guid}/minimum-selling-price")]
    public async Task<IActionResult> SetMinimumSellingPrice(
        Guid branchId,
        Guid productId,
        [FromBody] UpdateBranchPriceDto request,
        CancellationToken cancellationToken)
    {
        if (!await _branchAccessService.CanAccessAsync(
                User, branchId, cancellationToken))
        {
            return Forbid();
        }

        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await _priceService.SetMinimumSellingPriceAsync(
            branchId,
            productId,
            request,
            actorUserId,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{productId:guid}/history")]
    public async Task<IActionResult> GetHistory(
        Guid branchId,
        Guid productId,
        [FromQuery] PriceHistoryQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!await _branchAccessService.CanAccessAsync(
                User, branchId, cancellationToken))
        {
            return Forbid();
        }

        var history = await _priceService.GetHistoryAsync(
            branchId,
            productId,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        if (history is null)
        {
            return NotFound();
        }

        return Ok(history);
    }

    private IActionResult ToActionResult(BranchPriceResult result)
    {
        return result.Status switch
        {
            BranchPriceResultStatus.Success
                when result.Price is not null
                => Ok(result.Price),

            BranchPriceResultStatus.InvalidRequest
                => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid price update",
                    detail: result.Error),

            BranchPriceResultStatus.Conflict
                => Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Price update conflict",
                    detail: result.Error),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to update the price")
        };
    }
}