using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.StockTransfers;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/stock-transfers")]
public class StockTransfersController : ControllerBase
{
    private readonly IStockTransferService _transferService;

    public StockTransfersController(
        IStockTransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpPost]
    public async Task<ActionResult<StockTransferDto>> Create(
        [FromBody] CreateStockTransferDto request,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.CreateAsync(
            request,
            cancellationToken);

        if (result.Status != StockTransferResultStatus.Success)
        {
            return Failure(result);
        }

        var transfer = result.Transfer!;

        return CreatedAtAction(
            nameof(GetById),
            new { id = transfer.Id },
            transfer);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockTransferDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transfer = await _transferService.GetByIdAsync(
            id,
            cancellationToken);

        if (transfer is null)
        {
            return NotFound();
        }

        return Ok(transfer);
    }

    [HttpPost("{id:guid}/ship")]
    public async Task<ActionResult<StockTransferDto>> Ship(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _transferService.ShipAsync(
            id,
            cancellationToken));
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<StockTransferDto>> Receive(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _transferService.ReceiveAsync(
            id,
            cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<StockTransferDto>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _transferService.CancelAsync(
            id,
            cancellationToken));
    }

    private ActionResult<StockTransferDto> ToActionResult(
        StockTransferResult result)
    {
        if (result.Status != StockTransferResultStatus.Success)
        {
            return Failure(result);
        }

        return Ok(result.Transfer);
    }

    private ObjectResult Failure(StockTransferResult result)
    {
        var statusCode = result.Status switch
        {
            StockTransferResultStatus.NotFound =>
                StatusCodes.Status404NotFound,

            StockTransferResultStatus.InvalidRequest =>
                StatusCodes.Status400BadRequest,

            StockTransferResultStatus.Conflict =>
                StatusCodes.Status409Conflict,

            _ => throw new InvalidOperationException(
                "Unexpected stock transfer result.")
        };

        return Problem(
            statusCode: statusCode,
            title: "Transfer operation could not be completed.",
            detail: result.Error);
    }

    [HttpPost("{id:guid}/request-return")]
    public async Task<ActionResult<StockTransferDto>> RequestReturn(
    Guid id,
    [FromBody] RequestStockTransferReturnDto request,
    CancellationToken cancellationToken)
    {
        return ToActionResult(
            await _transferService.RequestReturnAsync(
                id,
                request,
                cancellationToken));
    }

    [HttpPost("{id:guid}/confirm-return")]
    public async Task<ActionResult<StockTransferDto>> ConfirmReturn(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await _transferService.ConfirmReturnAsync(
                id,
                cancellationToken));
    }
}