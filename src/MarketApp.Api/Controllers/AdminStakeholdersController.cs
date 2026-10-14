using System.Security.Claims;
using MarketApp.Application.Common.Results;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Users;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/admin/stakeholders")]
[Authorize(Roles = AppRoles.SystemAdmin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminStakeholdersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public AdminStakeholdersController(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStakeholderRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(actorClaim, out var actorId))
        {
            return Unauthorized();
        }

        var result = await _userManagementService.CreateStakeholderAsync(
            request,
            actorId,
            cancellationToken);

        return result.Status switch
        {
            UserManagementResultStatus.Success
                when result.User is not null
                => StatusCode(
                    StatusCodes.Status201Created,
                    result.User),

            UserManagementResultStatus.InvalidRequest
                => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid stakeholder details",
                    detail: result.Error),

            UserManagementResultStatus.Conflict
                => Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Stakeholder creation conflict",
                    detail: result.Error),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to create the stakeholder account")
        };
    }
}