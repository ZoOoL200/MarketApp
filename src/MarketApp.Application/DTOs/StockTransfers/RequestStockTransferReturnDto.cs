using System.ComponentModel.DataAnnotations;

namespace MarketApp.Application.DTOs.StockTransfers;

public class RequestStockTransferReturnDto
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}