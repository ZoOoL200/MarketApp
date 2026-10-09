using System.ComponentModel.DataAnnotations;
namespace MarketApp.Application.DTOs.Sales;
public sealed class SetProfitShareDto
{
    public Guid ClientChangeId { get; set; }
    public Guid ManagerUserId { get; set; }
    [Required, Range(typeof(decimal), "0", "100")] public decimal? Percent { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
}
public record ProfitShareDto(Guid Id, Guid ClientChangeId, Guid ManagerUserId, decimal Percent,
    DateTime EffectiveFromUtc, Guid CreatedByUserId, string Reason);
public record ManagerEarningsDto(Guid ManagerUserId, decimal Amount);
