using System.Security.Claims;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MarketApp.Api.Controllers;
[ApiController, Route("api/stock-adjustments"), Authorize(Roles = AppRoles.Stakeholder)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StockAdjustmentsController(IInventoryOperationsService service, IInventoryOperationsQueries queries) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AdjustmentDto), 200)]
    public async Task<IActionResult> Create(CreateAdjustmentDto request, CancellationToken ct)
        => Ok(await service.AdjustAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), request, ct));
    [HttpGet]
    [ProducesResponseType(typeof(MarketApp.Application.Common.PagedResult<AdjustmentDto>), 200)]
    public async Task<IActionResult> List([FromQuery] PageQuery page, CancellationToken ct)
        => Ok(await queries.AdjustmentsAsync(page, ct));
}
