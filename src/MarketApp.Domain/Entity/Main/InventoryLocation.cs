namespace MarketApp.Domain.Entity.Main;

public class InventoryLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public Guid? BranchId { get; set; }

    // Navigation property for the related Branch entity
    public Branch? Branch { get; set; }

    public bool IsActive { get; set; } = true;
}