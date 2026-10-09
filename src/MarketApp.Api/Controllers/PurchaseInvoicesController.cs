using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Purchases;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/purchase-invoices")]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly IPurchaseInvoiceService _purchaseService;

    public PurchaseInvoicesController(
        IPurchaseInvoiceService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MarketApp.Application.Common.PagedResult<MarketApp.Application.DTOs.Purchases.PurchaseInvoiceDto>), 200)]
    public async Task<IActionResult> List([FromQuery] MarketApp.Application.DTOs.Sales.PageQuery page, CancellationToken ct)
        => Ok(await _purchaseService.GetPageAsync(page, ct));

    [HttpPost]
    public async Task<ActionResult<PurchaseInvoiceDto>> Create(
        [FromBody] CreatePurchaseInvoiceDto request,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.CreateAsync(
            request,
            cancellationToken);

        if (result.Status != PurchaseInvoiceResultStatus.Success)
        {
            return Failure(result);
        }

        var invoice = result.Invoice!;

        return CreatedAtAction(
            nameof(GetById),
            new { id = invoice.Id },
            invoice);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var invoice = await _purchaseService.GetByIdAsync(
            id,
            cancellationToken);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(invoice);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<PurchaseInvoiceDto>> PostInvoice(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.PostAsync(
            id,
            cancellationToken);

        if (result.Status != PurchaseInvoiceResultStatus.Success)
        {
            return Failure(result);
        }

        return Ok(result.Invoice);
    }

    private ObjectResult Failure(PurchaseInvoiceResult result)
    {
        var statusCode = result.Status switch
        {
            PurchaseInvoiceResultStatus.NotFound =>
                StatusCodes.Status404NotFound,

            PurchaseInvoiceResultStatus.InvalidRequest =>
                StatusCodes.Status400BadRequest,

            PurchaseInvoiceResultStatus.Conflict =>
                StatusCodes.Status409Conflict,

            _ => throw new InvalidOperationException(
                "Unexpected purchase invoice result.")
        };

        return Problem(
            statusCode: statusCode,
            title: "Purchase operation could not be completed.",
            detail: result.Error);
    }
}