namespace MarketApp.Domain.Entity.Main;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;


    // Navigation property
    public ICollection<Product> Products { get; set; }
        = new List<Product>();
}
