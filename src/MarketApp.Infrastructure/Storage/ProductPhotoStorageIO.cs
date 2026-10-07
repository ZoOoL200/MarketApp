using System.Text.RegularExpressions;
using MarketApp.Application.Common;

namespace MarketApp.Infrastructure.Storage;

internal static class ProductPhotoStorageIO
{
    public static void ValidateKey(string storageKey)
    {
        if (storageKey is null || !Regex.IsMatch(storageKey,
                @"\Aproducts/[0-9a-f]{32}/[0-9a-f]{32}\.webp\z"))
        {
            throw new InvalidOperationException("Invalid product photo storage key.");
        }
    }

    public static async Task<byte[]> ReadBoundedAsync(Stream input, CancellationToken ct)
    {
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(), ct)) != 0)
        {
            if (output.Length + read > ProductPhotoLimits.MaximumFileBytes)
            {
                throw new InvalidDataException("Stored photo exceeds the supported size.");
            }
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        return output.ToArray();
    }
}
