using MarketApp.Application.Common.Results;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Users;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = AppRoles.Stakeholder)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MarketApp.Application.Common.PagedResult<MarketApp.Application.DTOs.Users.UserDetailsDto>), 200)]
    public async Task<IActionResult> List([FromQuery] MarketApp.Application.DTOs.Sales.PageQuery page, CancellationToken ct)
        => Ok(await _userManagementService.GetPageAsync(page.PageNumber, page.PageSize, ct));

    [HttpPut("{id:guid}/access")]
    [ProducesResponseType(typeof(MarketApp.Application.DTOs.Users.UserDetailsDto), 200)]
    public async Task<IActionResult> UpdateAccess(Guid id, UpdateUserAccessDto request, CancellationToken ct)
        => Ok(await _userManagementService.UpdateAccessAsync(id,
            Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), request, ct));

    [HttpPost]
    [ProducesResponseType(typeof(MarketApp.Application.DTOs.Users.UserDetailsDto), 201)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.CreateAsync(
            request,
            cancellationToken);

        return result.Status switch
        {
            UserManagementResultStatus.Success
                when result.User is not null
                => CreatedAtAction(
                    nameof(GetById),
                    new { id = result.User.Id },
                    result.User),

            UserManagementResultStatus.InvalidRequest
                => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid user details",
                    detail: result.Error),

            UserManagementResultStatus.Conflict
                => Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Account creation conflict",
                    detail: result.Error),

            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to create the account")
        };
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MarketApp.Application.DTOs.Users.UserDetailsDto), 200)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetByIdAsync(
            id,
            cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}