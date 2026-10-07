namespace MarketApp.Application.Common;

public static class ProductPhotoLimits
{
    public const int MaximumFileBytes = 5 * 1024 * 1024;
    public const long MaximumRequestBytes = 6L * 1024 * 1024;
    public const long MaximumPixels = 20_000_000;
    public const int MaximumDisplaySize = 1600;
}
