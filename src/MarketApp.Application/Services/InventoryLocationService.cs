using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.InventoryLocations;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Main;

namespace MarketApp.Application.Services;

public class InventoryLocationService : IInventoryLocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryLocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<InventoryLocationDto>> GetPageAsync(
        InventoryLocationQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var page = await _unitOfWork.InventoryLocations.GetPageAsync(
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            orderBy: locations => locations
                .OrderBy(l => l.Name)
                .ThenBy(l => l.Id),
            predicate: l =>
                (!query.BranchId.HasValue ||
                    l.BranchId == query.BranchId.Value) &&
                (!query.IsActive.HasValue ||
                    l.IsActive == query.IsActive.Value),
            includes: [l => l.Branch!],
            cancellationToken: cancellationToken);

        var items = page.Items
            .Select(l => ToDto(l, l.Branch?.Name))
            .ToList();

        return new PagedResult<InventoryLocationDto>(
            items,
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }

    public async Task<InventoryLocationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var location = await _unitOfWork.InventoryLocations.FindAsync(
            predicate: l => l.Id == id,
            includes: [l => l.Branch!],
            cancellationToken: cancellationToken);

        return location is null
            ? null
            : ToDto(location, location.Branch?.Name);
    }

    public async Task<InventoryLocationSaveResult> CreateAsync(
        CreateInventoryLocationDto request,
        CancellationToken cancellationToken = default)
    {
        string? branchName = null;

        if (request.BranchId.HasValue)
        {
            var branch = await _unitOfWork.Branches.FindAsync(
                predicate: b => b.Id == request.BranchId.Value,
                cancellationToken: cancellationToken);

            if (branch is null)
            {
                return new(
                    InventoryLocationSaveStatus.BranchNotFound);
            }

            if (!branch.IsActive)
            {
                return new(
                    InventoryLocationSaveStatus.BranchInactive);
            }

            branchName = branch.Name;
        }

        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _unitOfWork.InventoryLocations.AnyAsync(
            predicate: l => l.Code == code,
            cancellationToken: cancellationToken);

        if (codeExists)
        {
            return new(
                InventoryLocationSaveStatus.DuplicateCode);
        }

        var location = new InventoryLocation
        {
            Name = request.Name.Trim(),
            Code = code,
            BranchId = request.BranchId,
            IsActive = request.IsActive
        };

        await _unitOfWork.InventoryLocations.AddAsync(
            location,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            InventoryLocationSaveStatus.Success,
            ToDto(location, branchName));
    }

    public async Task<InventoryLocationSaveResult> UpdateAsync(
        Guid id,
        SaveInventoryLocationDto request,
        CancellationToken cancellationToken = default)
    {
        var location = await _unitOfWork.InventoryLocations.FindAsync(
            predicate: l => l.Id == id,
            includes: [l => l.Branch!],
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (location is null)
        {
            return new(
                InventoryLocationSaveStatus.NotFound);
        }

        // An inactive branch cannot have a location activated
        // through this endpoint. Deactivation remains allowed.
        if (request.IsActive &&
            location.BranchId.HasValue &&
            location.Branch?.IsActive != true)
        {
            return new(
                InventoryLocationSaveStatus.BranchInactive);
        }

        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _unitOfWork.InventoryLocations.AnyAsync(
            predicate: l => l.Code == code && l.Id != id,
            cancellationToken: cancellationToken);

        if (codeExists)
        {
            return new(
                InventoryLocationSaveStatus.DuplicateCode);
        }

        location.Name = request.Name.Trim();
        location.Code = code;
        location.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            InventoryLocationSaveStatus.Success,
            ToDto(location, location.Branch?.Name));
    }

    private static InventoryLocationDto ToDto(
        InventoryLocation location,
        string? branchName)
    {
        return new InventoryLocationDto(
            location.Id,
            location.Name,
            location.Code,
            location.BranchId,
            branchName,
            location.IsActive);
    }
}