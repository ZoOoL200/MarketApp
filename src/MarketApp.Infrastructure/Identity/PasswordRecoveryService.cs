using System.ComponentModel.DataAnnotations;
using System.Text;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace MarketApp.Infrastructure.Identity;

public class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationEmailSender _emailSender;
    private readonly ILogger<PasswordRecoveryService> _logger;

    public PasswordRecoveryService(
        UserManager<ApplicationUser> userManager,
        IApplicationEmailSender emailSender,
        ILogger<PasswordRecoveryService> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task RequestAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName)
            || userName.Length > 256)
        {
            return;
        }

        var user = await _userManager.FindByNameAsync(userName.Trim());

        if (user is null
            || !user.IsActive
            || !user.EmailConfirmed
            || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var token =
            await _userManager.GeneratePasswordResetTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));

        var body = $"""
            A password reset was requested for your MarketApp account.

            This reset token expires after one hour.

            UserId:
            {user.Id}

            Token:
            {encodedToken}

            If you did not request this, ignore this message.
            Your password has not been changed.
            """;

        try
        {
            await _emailSender.SendAsync(
                user.Email,
                "Reset your MarketApp password",
                body,
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            // Preserve the generic public response.
            // Do not log the token, password, or email body.
            _logger.LogError(
                "Password recovery email delivery failed. Error type: {ErrorType}",
                exception.GetType().Name);
        }
    }

    public async Task<bool> ResetAsync(
        ResetPasswordRequestDto request)
    {
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                validationResults,
                validateAllProperties: true)
            || request.UserId == Guid.Empty)
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(
            request.UserId.ToString());

        if (user is null
            || !user.IsActive
            || !user.EmailConfirmed
            || string.IsNullOrWhiteSpace(user.Email))
        {
            return false;
        }

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(request.Token));
        }
        catch (FormatException)
        {
            return false;
        }

        var result = await _userManager.ResetPasswordAsync(
            user,
            decodedToken,
            request.NewPassword);

        return result.Succeeded;
    }
}