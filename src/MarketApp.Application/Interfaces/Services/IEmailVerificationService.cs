using MarketApp.Application.DTOs.Authentication;

namespace MarketApp.Application.Interfaces.Services;

public interface IEmailVerificationService
{
    Task RequestAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmAsync(ConfirmEmailRequestDto request);
}