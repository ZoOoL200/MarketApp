using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Suppliers;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Main;

namespace MarketApp.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<SupplierDto>> GetPageAsync(
        SupplierQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var page = await _unitOfWork.Suppliers.GetPageAsync(
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            orderBy: suppliers => suppliers
                .OrderBy(s => s.Name)
                .ThenBy(s => s.Id),
            predicate: s =>
                !query.IsActive.HasValue ||
                s.IsActive == query.IsActive.Value,
            cancellationToken: cancellationToken);

        var items = page.Items
            .Select(ToDto)
            .ToList();

        return new PagedResult<SupplierDto>(
            items,
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }

    public async Task<SupplierDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var supplier = await _unitOfWork.Suppliers.FindAsync(
            predicate: s => s.Id == id,
            cancellationToken: cancellationToken);

        return supplier is null ? null : ToDto(supplier);
    }

    public async Task<SupplierSaveResult> CreateAsync(
        SaveSupplierDto request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _unitOfWork.Suppliers.AnyAsync(
            predicate: s => s.Code == code,
            cancellationToken: cancellationToken);

        if (codeExists)
        {
            return new(SupplierSaveStatus.DuplicateCode);
        }

        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            Code = code,
            Phone = NormalizeOptional(request.Phone),
            Email = NormalizeOptional(request.Email),
            Address = NormalizeOptional(request.Address),
            IsActive = request.IsActive
        };

        await _unitOfWork.Suppliers.AddAsync(
            supplier,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            SupplierSaveStatus.Success,
            ToDto(supplier));
    }

    public async Task<SupplierSaveResult> UpdateAsync(
        Guid id,
        SaveSupplierDto request,
        CancellationToken cancellationToken = default)
    {
        var supplier = await _unitOfWork.Suppliers.FindAsync(
            predicate: s => s.Id == id,
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (supplier is null)
        {
            return new(SupplierSaveStatus.NotFound);
        }

        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _unitOfWork.Suppliers.AnyAsync(
            predicate: s => s.Code == code && s.Id != id,
            cancellationToken: cancellationToken);

        if (codeExists)
        {
            return new(SupplierSaveStatus.DuplicateCode);
        }

        supplier.Name = request.Name.Trim();
        supplier.Code = code;
        supplier.Phone = NormalizeOptional(request.Phone);
        supplier.Email = NormalizeOptional(request.Email);
        supplier.Address = NormalizeOptional(request.Address);
        supplier.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            SupplierSaveStatus.Success,
            ToDto(supplier));
    }

    private static SupplierDto ToDto(Supplier supplier)
    {
        return new SupplierDto(
            supplier.Id,
            supplier.Name,
            supplier.Code,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.IsActive);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}