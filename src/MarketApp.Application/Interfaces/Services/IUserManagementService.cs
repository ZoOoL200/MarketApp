using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Users;

namespace MarketApp.Application.Interfaces.Services;

public interface IUserManagementService
{
    Task<UserManagementResult> CreateAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UserDetailsDto?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}