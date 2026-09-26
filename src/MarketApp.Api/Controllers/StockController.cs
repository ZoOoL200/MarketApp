using MarketApp.Application.Common;
using MarketApp.Application.DTOs.Stock;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/stock")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpGet("balances")]
    public async Task<ActionResult<PagedResult<StockBalanceDto>>>
        GetBalances(
            [FromQuery] StockQueryDto query,
            CancellationToken cancellationToken)
    {
        var page = await _stockService.GetBalancesAsync(
            query,
            cancellationToken);

        return Ok(page);
    }

    [HttpGet("movements")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>>
        GetMovements(
            [FromQuery] StockQueryDto query,
            CancellationToken cancellationToken)
    {
        var page = await _stockService.GetMovementsAsync(
            query,
            cancellationToken);

        return Ok(page);
    }
}