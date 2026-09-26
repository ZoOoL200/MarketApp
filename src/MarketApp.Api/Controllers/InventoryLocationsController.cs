using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.InventoryLocations;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/inventory-locations")]
public class InventoryLocationsController : ControllerBase
{
    private readonly IInventoryLocationService _locationService;

    public InventoryLocationsController(
        IInventoryLocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryLocationDto>>>
        GetPage(
            [FromQuery] InventoryLocationQueryDto query,
            CancellationToken cancellationToken)
    {
        var page = await _locationService.GetPageAsync(
            query,
            cancellationToken);

        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryLocationDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var location = await _locationService.GetByIdAsync(
            id,
            cancellationToken);

        if (location is null)
        {
            return NotFound();
        }

        return Ok(location);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryLocationDto>> Create(
        [FromBody] CreateInventoryLocationDto request,
        CancellationToken cancellationToken)
    {
        var result = await _locationService.CreateAsync(
            request,
            cancellationToken);

        if (result.Status != InventoryLocationSaveStatus.Success)
        {
            return SaveFailure(result.Status);
        }

        var location = result.Location!;

        return CreatedAtAction(
            nameof(GetById),
            new { id = location.Id },
            location);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryLocationDto>> Update(
        Guid id,
        [FromBody] SaveInventoryLocationDto request,
        CancellationToken cancellationToken)
    {
        var result = await _locationService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result.Status != InventoryLocationSaveStatus.Success)
        {
            return SaveFailure(result.Status);
        }

        return Ok(result.Location);
    }

    private ObjectResult SaveFailure(
        InventoryLocationSaveStatus status)
    {
        return status switch
        {
            InventoryLocationSaveStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Inventory location not found."),

            InventoryLocationSaveStatus.DuplicateCode => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Location code already exists.",
                detail: "Another inventory location uses this code."),

            InventoryLocationSaveStatus.BranchNotFound => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid branch.",
                detail: "Choose an existing branch, or use null " +
                        "for a central location."),

            InventoryLocationSaveStatus.BranchInactive => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Branch is inactive.",
                detail: "Activate the branch before creating a " +
                        "location or saving an active location."),

            _ => throw new InvalidOperationException(
                "Unexpected inventory location save result.")
        };
    }
}