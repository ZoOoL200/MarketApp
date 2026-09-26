using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Branches;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/branches")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BranchDto>>> GetPage(
        [FromQuery] BranchQueryDto query,
        CancellationToken cancellationToken)
    {
        var page = await _branchService.GetPageAsync(
            query,
            cancellationToken);

        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BranchDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var branch = await _branchService.GetByIdAsync(
            id,
            cancellationToken);

        if (branch is null)
        {
            return NotFound();
        }

        return Ok(branch);
    }

    [HttpPost]
    public async Task<ActionResult<BranchDto>> Create(
        [FromBody] SaveBranchDto request,
        CancellationToken cancellationToken)
    {
        var result = await _branchService.CreateAsync(
            request,
            cancellationToken);

        if (result.Status != BranchSaveStatus.Success)
        {
            return SaveFailure(result.Status);
        }

        var branch = result.Branch!;

        return CreatedAtAction(
            nameof(GetById),
            new { id = branch.Id },
            branch);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BranchDto>> Update(
        Guid id,
        [FromBody] SaveBranchDto request,
        CancellationToken cancellationToken)
    {
        var result = await _branchService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result.Status != BranchSaveStatus.Success)
        {
            return SaveFailure(result.Status);
        }

        return Ok(result.Branch);
    }

    private ObjectResult SaveFailure(BranchSaveStatus status)
    {
        return status switch
        {
            BranchSaveStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Branch not found."),

            BranchSaveStatus.DuplicateCode => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Branch code already exists.",
                detail: "Another branch uses this code."),

            _ => throw new InvalidOperationException(
                "Unexpected branch save result.")
        };
    }
}