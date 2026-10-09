using System.ComponentModel.DataAnnotations;
using MarketApp.Application.Common;
namespace MarketApp.Application.DTOs.Accounting;

public sealed class CreateStakeholderExpenseDto
{
    public Guid ClientExpenseId { get; set; }
    public Guid? BranchId { get; set; }
    [Required, RegularExpression("^(Rent|Other)$")] public string Category { get; set; } = "Other";
    [Range(typeof(decimal), "0.0001", "1000000000000")] public decimal Amount { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Description { get; set; } = "";
    [StringLength(200)] public string? PaidTo { get; set; }
    [StringLength(100)] public string? ReferenceNumber { get; set; }
}
public sealed class VoidStakeholderExpenseDto
{
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
}
public sealed class StakeholderExpenseQuery
{
    [Range(1, 1000000)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 25;
    public Guid? BranchId { get; set; }
    [RegularExpression("^(Rent|Other)$")] public string? Category { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    [StringLength(200)] public string? Search { get; set; }
    public bool IncludeVoided { get; set; }
}
public record StakeholderExpenseDto(Guid Id, Guid ClientExpenseId, Guid? BranchId, string? BranchName,
    string Category, decimal Amount, DateTime OccurredAtUtc, string Description, string? PaidTo,
    string? ReferenceNumber, Guid CreatedByUserId, DateTime CreatedAtUtc,
    Guid? VoidedByUserId, DateTime? VoidedAtUtc, string? VoidReason);
public record StakeholderExpensePageDto(IReadOnlyList<StakeholderExpenseDto> Items, int TotalCount,
    int PageNumber, int PageSize, decimal ActiveAmount)
    : PagedResult<StakeholderExpenseDto>(Items, TotalCount, PageNumber, PageSize);
