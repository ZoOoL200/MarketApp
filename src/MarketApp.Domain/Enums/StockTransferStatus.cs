namespace MarketApp.Domain.Enums;

public enum StockTransferStatus
{
    Draft = 1,
    InTransit = 2,
    Received = 3,
    Cancelled = 4,
    ReturnRequested = 5,
    Returned = 6
}