namespace MarketApp.Domain.Enums;

public enum StockMovementType
{
    PurchaseReceipt = 1,
    TransferOut = 2,
    TransferIn = 3,
    TransferReturn = 4,
    Sale = 5,
    SalesReturn = 6,
    Adjustment = 7
}