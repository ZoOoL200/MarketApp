using MarketApp.Application.Common;
using SkiaSharp;

namespace MarketApp.Infrastructure.Photos;

internal static class ProductPhotoProcessor
{
    public static async Task<(byte[]? Bytes, string? Error)> NormalizeAsync(
        Stream input, CancellationToken cancellationToken)
    {
        using var source = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
        {
            if (source.Length + read > ProductPhotoLimits.MaximumFileBytes)
                return (null, "The photo must not exceed 5 MB.");
            await source.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (source.Length == 0) return (null, "Choose a non-empty image file.");

        cancellationToken.ThrowIfCancellationRequested();
        using var data = SKData.CreateCopy(source.ToArray());
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat is not
            (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
        {
            return (null, "Choose a valid JPEG, PNG, or WebP image.");
        }
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 ||
            (long)info.Width * info.Height > ProductPhotoLimits.MaximumPixels)
            return (null, "The image must not exceed 20 million pixels.");

        // Decode only the first frame, converting to a bounded sRGB bitmap.
        using var colorSpace = SKColorSpace.CreateSrgb();
        using var decoded = new SKBitmap(new SKImageInfo(info.Width, info.Height,
            SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace));
        if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success)
            return (null, "The file is not a complete valid image.");
        cancellationToken.ThrowIfCancellationRequested();

        var rotated = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = rotated ? info.Height : info.Width;
        var height = rotated ? info.Width : info.Height;
        var scale = Math.Min(1d, (double)ProductPhotoLimits.MaximumDisplaySize / Math.Max(width, height));
        var outputWidth = Math.Max(1, (int)Math.Round(width * scale));
        var outputHeight = Math.Max(1, (int)Math.Round(height * scale));
        using var output = new SKBitmap(new SKImageInfo(outputWidth, outputHeight,
            SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace));
        using (var canvas = new SKCanvas(output))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)outputWidth / width, (float)outputHeight / height);
            ApplyOrientation(canvas, codec.EncodedOrigin, info.Width, info.Height);
            using var image = SKImage.FromBitmap(decoded);
            canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        }
        cancellationToken.ThrowIfCancellationRequested();
        // Encoding a fresh bitmap omits uploaded EXIF/GPS and other source metadata.
        using var normalized = SKImage.FromBitmap(output);
        using var encoded = normalized.Encode(SKEncodedImageFormat.Webp, 82);
        if (encoded is null) throw new InvalidOperationException("WebP encoding failed.");
        if (encoded.Size > ProductPhotoLimits.MaximumFileBytes)
            return (null, "The processed image is too large.");
        return (encoded.ToArray(), null);
    }

    private static void ApplyOrientation(SKCanvas canvas, SKEncodedOrigin origin, int width, int height)
    {
        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Translate(width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight:
                canvas.Translate(width, height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(height, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(height, width); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, width); canvas.RotateDegrees(270); break;
        }
    }
}
