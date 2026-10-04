using MarketApp.Application.Common.Security;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/branches/{branchId:guid}/selling-prices")]
[Authorize(
    Policy = "AuthenticatedUser",
    Roles = AppRoles.Stakeholder + ","
        + AppRoles.BranchManager + ","
        + AppRoles.Seller)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SellingPricesController : ControllerBase
{
    private readonly IBranchProductPriceService _priceService;
    private readonly IBranchAccessService _branchAccessService;

    public SellingPricesController(
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

        var price = await _priceService.GetSellerPriceAsync(
            branchId,
            productId,
            cancellationToken);

        if (price is null)
        {
            return NotFound();
        }

        return Ok(price);
    }
}