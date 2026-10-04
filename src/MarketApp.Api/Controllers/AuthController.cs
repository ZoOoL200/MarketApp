using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IAuthSessionService _authSessionService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public AuthController(
        IIdentityService identityService,
        IAuthSessionService authSessionService,
        IEmailVerificationService emailVerificationService,
        IPasswordRecoveryService passwordRecoveryService)
    {
        _identityService = identityService;
        _authSessionService = authSessionService;
        _emailVerificationService = emailVerificationService;
        _passwordRecoveryService = passwordRecoveryService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var principal =
            await _identityService.AuthenticateAsync(request);

        if (principal is null)
        {
            return AuthenticationFailed();
        }

        var response = await _authSessionService.CreateAsync(
            principal,
            cancellationToken);

        if (response is null)
        {
            return AuthenticationFailed();
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authSessionService.RefreshAsync(
            request.RefreshToken,
            cancellationToken);

        if (response is null)
        {
            return AuthenticationFailed();
        }

        return Ok(response);
    }

    [Authorize(Policy = "AuthenticatedUser")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        await _authSessionService.LogoutAsync(
            User,
            cancellationToken);

        return NoContent();
    }

    [Authorize(Policy = "AuthenticatedUser")]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            userName = User.Identity?.Name,
            sessionId = User.FindFirstValue(AppClaimTypes.SessionId),
            roles = User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Distinct()
                .ToArray()
        });
    }

    private ObjectResult AuthenticationFailed()
    {
        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authentication failed",
            detail: "The credentials or session are invalid or unavailable.");
    }

    [Authorize(Policy = "AuthenticatedUser")]
    [HttpPost("request-email-verification")]
    public async Task<IActionResult> RequestEmailVerification(
    CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
        {
            return Unauthorized();
        }

        await _emailVerificationService.RequestAsync(
            userId,
            cancellationToken);

        return Ok(new
        {
            message = "If your email needs verification, "
                + "a verification message has been sent."
        });
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequestDto request)
    {
        var succeeded =
            await _emailVerificationService.ConfirmAsync(request);

        if (!succeeded)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Email verification failed",
                detail: "The verification details are invalid or expired.");
        }

        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting("PasswordRecovery")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
    [FromBody] ForgotPasswordRequestDto request,
    CancellationToken cancellationToken)
    {
        await _passwordRecoveryService.RequestAsync(
            request.UserName,
            cancellationToken);

        return Ok(new
        {
            message = "If the account is eligible for password recovery, "
                + "a reset message will be sent to its verified email."
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("PasswordRecovery")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request)
    {
        var succeeded =
            await _passwordRecoveryService.ResetAsync(request);

        if (!succeeded)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password reset failed",
                detail: "The reset details are invalid or expired, "
                    + "or the new password does not meet the requirements.");
        }

        return NoContent();
    }
}