using AStarDev.ScraperPlaying.WallpaperIngestion;
using SkiaSharp;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenADownloadedImageDecoder
{
    private static readonly byte[] OnePixelPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGP4z8AAAAMBAQDJ/pLvAAAAAElFTkSuQmCC");
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly MockFileSystem fileSystem = new();
    private readonly DownloadedImageDecoder decoder;

    public GivenADownloadedImageDecoder()
        => decoder = new(fileSystem);

    [Fact]
    public void when_decoding_a_valid_image_file_then_a_png_encoded_stream_is_returned()
    {
        fileSystem.Directory.CreateDirectory("/images");
        fileSystem.File.WriteAllBytes("/images/wallpaper-1.png", OnePixelPng);

        using var result = decoder.DecodeToPng("/images/wallpaper-1.png", 1280);

        var signature = new byte[8];
        result.ReadExactly(signature);
        signature.ShouldBe(PngSignature);
    }

    [Fact]
    public void when_decoding_a_file_that_is_not_an_image_then_it_throws()
    {
        fileSystem.Directory.CreateDirectory("/images");
        fileSystem.File.WriteAllBytes("/images/not-an-image.txt", "this is not an image"u8.ToArray());

        Should.Throw<InvalidOperationException>(() => decoder.DecodeToPng("/images/not-an-image.txt", 1280));
    }

    [Theory]
    [InlineData(400, 200, 100, 100, 50)]
    [InlineData(200, 400, 100, 50, 100)]
    [InlineData(400, 200, 400, 400, 200)]
    [InlineData(400, 200, 1280, 400, 200)]
    public void when_the_image_is_larger_than_the_maximum_dimension_then_it_is_scaled_down_but_never_up(int width, int height, int maxDimension, int expectedWidth, int expectedHeight)
    {
        fileSystem.Directory.CreateDirectory("/images");
        fileSystem.File.WriteAllBytes("/images/wallpaper-1.png", CreatePng(width, height));

        using var result = decoder.DecodeToPng("/images/wallpaper-1.png", maxDimension);

        using var decoded = SKBitmap.Decode(result);
        decoded.Width.ShouldBe(expectedWidth);
        decoded.Height.ShouldBe(expectedHeight);
    }

    [Theory]
    [InlineData(4000, 2000, 1280, 1280, 640)]
    [InlineData(2000, 4000, 1280, 640, 1280)]
    [InlineData(300, 200, 1280, 300, 200)]
    public void when_a_jpeg_is_larger_than_the_maximum_dimension_then_it_is_scaled_down_to_fit_but_never_up(int width, int height, int maxDimension, int expectedWidth, int expectedHeight)
    {
        fileSystem.Directory.CreateDirectory("/images");
        fileSystem.File.WriteAllBytes("/images/wallpaper-1.jpg", CreateImage(width, height, SKEncodedImageFormat.Jpeg));

        using var result = decoder.DecodeToPng("/images/wallpaper-1.jpg", maxDimension);

        using var decoded = SKBitmap.Decode(result);
        decoded.Width.ShouldBe(expectedWidth);
        decoded.Height.ShouldBe(expectedHeight);
    }

    private static byte[] CreatePng(int width, int height) => CreateImage(width, height, SKEncodedImageFormat.Png);

    private static byte[] CreateImage(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);

        return data.ToArray();
    }
}
