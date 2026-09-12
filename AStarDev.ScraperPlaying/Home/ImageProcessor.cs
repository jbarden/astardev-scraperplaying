using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public class ImageProcessor(Func<DateTimeOffset> clock, IFileSystem fileSystem, Func<TimeSpan> pacingDelay, IUnitOfWork unitOfWork) : IImageProcessor
{
    private const string TopWallpapersDirectorySegment = "top-wallpapers";

    /// <inheritdoc/>
    public async Task DownloadImageAsync(string id, string imageUri, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
    {
        await Task.Delay(pacingDelay(), cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, imageUri);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        progress.Report($"Downloading image for wallpaper {id} from {imageUri}");
        using Stream downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var fileStream = fileSystem.FileStream.New($"{id}.jpg", FileMode.Create, FileAccess.Write, FileShare.None);

        await downloadStream.CopyToAsync(fileStream, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Exceptional<FileEntity>> ProcessTheImageAsync(IRepository<FileEntity, FileId> fileRepository, Data wallpaper, Option<string> categoryName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directorySegment = categoryName.Match(name => name.ToDirectorySlug(), () => TopWallpapersDirectorySegment);
        var rootDirectory = await LoadRootDirectoryAsync();

        var fileEntity = new FileEntity
        {
            Id = FileId.Empty,
            FileName = new FileName(wallpaper.Id),
            DirectoryName = DirectoryName.Create(fileSystem.Path.Combine(rootDirectory, directorySegment)),
            FileAccessDetail = new FileAccessDetailEntity
            {
                DetailsLastUpdated = clock().UtcDateTime,
                Id = FileAccessDetailId.Empty,
                FileId = FileId.Empty
            },
            FileSize = wallpaper.FileSize,
            FileHandle = FileHandle.Create(wallpaper.Id),
            FileType = wallpaper.FileType,
            IsImage = wallpaper.Path.IsImage(),
            ImageDetail = new ImageDetailEntity
            {
                Id = ImageId.Empty,
                FileId = FileId.Empty,
                Width = wallpaper.DimensionX,
                Height = wallpaper.DimensionY,
            }
        };

        return fileRepository.Add(fileEntity);
    }

    private async Task<string> LoadRootDirectoryAsync()
    {
        var configuration = (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
            .Match(
                option => option.Match(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                exception => throw exception
            );

        return configuration.ScrapeDirectories.RootDirectory;
    }
}
