using MarketApp.Domain.Entity.Main;

namespace MarketApp.Infrastructure.Identity;

public class ApplicationUserBranch
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
}