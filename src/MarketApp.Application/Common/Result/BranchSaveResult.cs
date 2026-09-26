using MarketApp.Application.DTOs.Branches;

namespace MarketApp.Application.Common.Results;

public enum BranchSaveStatus
{
    Success,
    NotFound,
    DuplicateCode
}

public record BranchSaveResult(
    BranchSaveStatus Status,
    BranchDto? Branch = null);