using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Suppliers;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierDto>>> GetPage(
        [FromQuery] SupplierQueryDto query,
        CancellationToken cancellationToken)
    {
        var page = await _supplierService.GetPageAsync(
            query,
            cancellationToken);

        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(
            id,
            cancellationToken);

        if (supplier is null)
        {
            return NotFound();
        }

        return Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(
        [FromBody] SaveSupplierDto request,
        CancellationToken cancellationToken)
    {
        var result = await _supplierService.CreateAsync(
            request,
            cancellationToken);

        if (result.Status != SupplierSaveStatus.Success)
        {
            return SaveFailure(result.Status);
        }

        var supplier = result.Supplier!;

        return CreatedAtAction(
            nameof(GetById),
            new { id = supplier.Id },
            supplier);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Update(
        Guid id,
        [FromBody] SaveSupplierDto request,
        CancellationToken cancellationToken)
    {
        var result = await _supplierService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result.Status != SupplierSaveStatus.Success)
        {
            return SaveFailure(result.Status);
        }

        return Ok(result.Supplier);
    }

    private ObjectResult SaveFailure(SupplierSaveStatus status)
    {
        return status switch
        {
            SupplierSaveStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Supplier not found."),

            SupplierSaveStatus.DuplicateCode => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Supplier code already exists.",
                detail: "Another supplier uses this code."),

            _ => throw new InvalidOperationException(
                "Unexpected supplier save result.")
        };
    }
}