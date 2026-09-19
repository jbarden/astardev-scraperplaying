using System.IO.Abstractions;
using SkiaSharp;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class DownloadedImageDecoder(IFileSystem fileSystem) : IDownloadedImageDecoder
{
    /// <summary>A low zlib level: the preview is shown once and discarded, so encoding speed matters far more than size.</summary>
    private const int PngCompressionLevel = 1;

    /// <inheritdoc/>
    public Stream DecodeToPng(string filePath, int maxDimension)
    {
        using var fileStream = fileSystem.File.OpenRead(filePath);
        using var codec = SKCodec.Create(fileStream) ?? throw new InvalidOperationException($"Unable to decode '{filePath}' as an image.");
        var scale = (double)maxDimension / Math.Max(codec.Info.Width, codec.Info.Height);
        using var bitmap = DecodeAtOrAboveScale(codec, scale) ?? throw new InvalidOperationException($"Unable to decode '{filePath}' as an image.");
        if (scale >= 1d) return EncodePng(bitmap);

        var fit = maxDimension / (double)Math.Max(bitmap.Width, bitmap.Height);
        if (fit >= 1d) return EncodePng(bitmap);

        using var resized = bitmap.Resize(new SKImageInfo(Math.Max(1, (int)(bitmap.Width * fit)), Math.Max(1, (int)(bitmap.Height * fit))), SKSamplingOptions.Default)
            ?? throw new InvalidOperationException($"Unable to scale '{filePath}' for preview.");

        return EncodePng(resized);
    }

    /// <summary>Decodes at the smallest size the codec can produce natively that is still at least <paramref name="scale"/> (formats such as JPEG skip most of the work), or at full size when it cannot scale.</summary>
    private static SKBitmap? DecodeAtOrAboveScale(SKCodec codec, double scale)
    {
        var size = scale < 1d ? codec.GetScaledDimensions((float)scale) : codec.Info.Size;

        return SKBitmap.Decode(codec, codec.Info.WithSize(size.Width, size.Height));
    }

    private static MemoryStream EncodePng(SKBitmap bitmap)
    {
        using var pixels = bitmap.PeekPixels() ?? throw new InvalidOperationException("Unable to read the decoded image pixels.");
        using var data = pixels.Encode(new SKPngEncoderOptions(SKPngEncoderFilterFlags.NoFilters, PngCompressionLevel)) ?? throw new InvalidOperationException("Unable to encode the preview image.");

        var pngStream = new MemoryStream();
        data.SaveTo(pngStream);
        pngStream.Position = 0;

        return pngStream;
    }
}
