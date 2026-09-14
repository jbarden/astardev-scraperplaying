using AStarDev.ScraperPlaying.Home;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit;

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

        using var result = decoder.DecodeToPng("/images/wallpaper-1.png");

        var signature = new byte[8];
        result.ReadExactly(signature);
        signature.ShouldBe(PngSignature);
    }

    [Fact]
    public void when_decoding_a_file_that_is_not_an_image_then_it_throws()
    {
        fileSystem.Directory.CreateDirectory("/images");
        fileSystem.File.WriteAllBytes("/images/not-an-image.txt", "this is not an image"u8.ToArray());

        Should.Throw<InvalidOperationException>(() => decoder.DecodeToPng("/images/not-an-image.txt"));
    }
}
