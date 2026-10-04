using System.Text;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace MarketApp.Infrastructure.Identity;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationEmailSender _emailSender;

    public EmailVerificationService(
        UserManager<ApplicationUser> userManager,
        IApplicationEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    public async Task RequestAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null
            || !user.IsActive
            || user.EmailConfirmed
            || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var token =
            await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));

        var body = $"""
            Verify your MarketApp email address.

            This verification token expires after one hour.

            UserId:
            {user.Id}

            Token:
            {encodedToken}

            If you did not request this message, you can ignore it.
            """;

        await _emailSender.SendAsync(
            user.Email,
            "Verify your MarketApp email",
            body,
            cancellationToken);
    }

    public async Task<bool> ConfirmAsync(
        ConfirmEmailRequestDto request)
    {
        if (request.UserId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Token)
            || request.Token.Length > 4096)
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(
            request.UserId.ToString());

        if (user is null
            || !user.IsActive
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

        var result = await _userManager.ConfirmEmailAsync(
            user,
            decodedToken);

        return result.Succeeded;
    }
}