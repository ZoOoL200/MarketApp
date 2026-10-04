using Microsoft.AspNetCore.Identity;

namespace MarketApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString();
    }

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUserBranch> BranchAssignments { get; set; }
        = new List<ApplicationUserBranch>();
}