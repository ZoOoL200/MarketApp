namespace MarketApp.Application.DTOs.InventoryLocations;

public class CreateInventoryLocationDto : SaveInventoryLocationDto
{
    public Guid? BranchId { get; set; }
}