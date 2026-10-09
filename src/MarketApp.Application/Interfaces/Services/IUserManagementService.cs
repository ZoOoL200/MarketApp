using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Users;

namespace MarketApp.Application.Interfaces.Services;

public interface IUserManagementService
{
    Task<MarketApp.Application.Common.PagedResult<UserDetailsDto>> GetPageAsync(int pageNumber, int pageSize, CancellationToken ct = default);
    Task<UserDetailsDto> UpdateAccessAsync(Guid userId, Guid actorId, UpdateUserAccessDto request, CancellationToken ct = default);

    Task<UserManagementResult> CreateAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UserDetailsDto?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}