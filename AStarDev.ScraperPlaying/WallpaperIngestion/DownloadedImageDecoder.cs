using System.IO.Abstractions;
using SkiaSharp;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class DownloadedImageDecoder(IFileSystem fileSystem) : IDownloadedImageDecoder
{
    /// <inheritdoc/>
    public Stream DecodeToPng(string filePath)
    {
        using var fileStream = fileSystem.File.OpenRead(filePath);
        using var bitmap = SKBitmap.Decode(fileStream) ?? throw new InvalidOperationException($"Unable to decode '{filePath}' as an image.");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var pngStream = new MemoryStream();
        data.SaveTo(pngStream);
        pngStream.Position = 0;

        return pngStream;
    }
}
