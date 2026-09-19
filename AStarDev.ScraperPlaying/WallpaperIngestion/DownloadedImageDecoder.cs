using System.IO.Abstractions;
using SkiaSharp;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class DownloadedImageDecoder(IFileSystem fileSystem) : IDownloadedImageDecoder
{
    /// <inheritdoc/>
    public Stream DecodeToPng(string filePath, int maxDimension)
    {
        using var fileStream = fileSystem.File.OpenRead(filePath);
        using var bitmap = SKBitmap.Decode(fileStream) ?? throw new InvalidOperationException($"Unable to decode '{filePath}' as an image.");
        var scale = (double)maxDimension / Math.Max(bitmap.Width, bitmap.Height);
        if (scale >= 1d) return EncodePng(bitmap);

        using var resized = bitmap.Resize(new SKImageInfo(Math.Max(1, (int)(bitmap.Width * scale)), Math.Max(1, (int)(bitmap.Height * scale))), SKSamplingOptions.Default)
            ?? throw new InvalidOperationException($"Unable to scale '{filePath}' for preview.");

        return EncodePng(resized);
    }

    private static MemoryStream EncodePng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var pngStream = new MemoryStream();
        data.SaveTo(pngStream);
        pngStream.Position = 0;

        return pngStream;
    }
}
