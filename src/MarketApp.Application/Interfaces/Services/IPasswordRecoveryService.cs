using MarketApp.Application.DTOs.Authentication;

namespace MarketApp.Application.Interfaces.Services;

public interface IPasswordRecoveryService
{
    Task RequestAsync(
        string userName,
        CancellationToken cancellationToken = default);

    Task<bool> ResetAsync(ResetPasswordRequestDto request);
}