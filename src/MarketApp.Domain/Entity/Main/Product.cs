namespace MarketApp.Domain.Entity.Main;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Guid CategoryId { get; set; }

    // Navigation property
    public Category Category { get; set; } = null!;
}
