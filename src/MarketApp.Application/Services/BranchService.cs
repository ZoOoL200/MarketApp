using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Branches;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Main;

namespace MarketApp.Application.Services;

public class BranchService : IBranchService
{
    private readonly IUnitOfWork _unitOfWork;

    public BranchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<BranchDto>> GetPageAsync(
        BranchQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var page = await _unitOfWork.Branches.GetPageAsync(
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            orderBy: branches => branches
                .OrderBy(b => b.Name)
                .ThenBy(b => b.Id),
            predicate: b =>
                !query.IsActive.HasValue ||
                b.IsActive == query.IsActive.Value,
            cancellationToken: cancellationToken);

        var items = page.Items
            .Select(ToDto)
            .ToList();

        return new PagedResult<BranchDto>(
            items,
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }

    public async Task<BranchDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var branch = await _unitOfWork.Branches.FindAsync(
            predicate: b => b.Id == id,
            cancellationToken: cancellationToken);

        return branch is null ? null : ToDto(branch);
    }

    public async Task<BranchSaveResult> CreateAsync(
        SaveBranchDto request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _unitOfWork.Branches.AnyAsync(
            predicate: b => b.Code == code,
            cancellationToken: cancellationToken);

        if (codeExists)
        {
            return new(BranchSaveStatus.DuplicateCode);
        }

        var branch = new Branch
        {
            Name = request.Name.Trim(),
            Code = code,
            Address = NormalizeOptional(request.Address),
            Phone = NormalizeOptional(request.Phone),
            IsActive = request.IsActive
        };

        await _unitOfWork.Branches.AddAsync(
            branch,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            BranchSaveStatus.Success,
            ToDto(branch));
    }

    public async Task<BranchSaveResult> UpdateAsync(
        Guid id,
        SaveBranchDto request,
        CancellationToken cancellationToken = default)
    {
        var branch = await _unitOfWork.Branches.FindAsync(
            predicate: b => b.Id == id,
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (branch is null)
        {
            return new(BranchSaveStatus.NotFound);
        }

        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _unitOfWork.Branches.AnyAsync(
            predicate: b => b.Code == code && b.Id != id,
            cancellationToken: cancellationToken);

        if (codeExists)
        {
            return new(BranchSaveStatus.DuplicateCode);
        }

        branch.Name = request.Name.Trim();
        branch.Code = code;
        branch.Address = NormalizeOptional(request.Address);
        branch.Phone = NormalizeOptional(request.Phone);
        branch.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            BranchSaveStatus.Success,
            ToDto(branch));
    }

    private static BranchDto ToDto(Branch branch)
    {
        return new BranchDto(
            branch.Id,
            branch.Name,
            branch.Code,
            branch.Address,
            branch.Phone,
            branch.IsActive);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}