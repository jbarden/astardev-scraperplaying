using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class ImageProcessor(Func<DateTimeOffset> clock, IFileSystem fileSystem, Func<TimeSpan> pacingDelay, IImageDownloadNotifier imageDownloadNotifier) : IImageProcessor
{
    /// <inheritdoc/>
    public async Task DownloadImageAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
    {
        await Task.Delay(pacingDelay(), cancellationToken);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.Wallpaper.Path);

        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        progress.Report($"Downloading image for wallpaper {request.Wallpaper.Id} from {request.Wallpaper.Path}");
        using Stream downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        fileSystem.Directory.CreateDirectory(request.Directory);
        var savedPath = fileSystem.Path.Combine(request.Directory, $"{request.Wallpaper.Id}{request.Extension}");
        using (var fileStream = fileSystem.FileStream.New(savedPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await downloadStream.CopyToAsync(fileStream, cancellationToken);
        }

        imageDownloadNotifier.NotifyImageDownloaded(new WallpaperDownloadDetails(savedPath, request.Wallpaper.Id, request.CategoryLabel, request.Wallpaper.FileSize, request.Wallpaper.DimensionX, request.Wallpaper.DimensionY));
    }

    /// <inheritdoc/>
    public Task<Exceptional<FileEntity>> ProcessTheImageAsync(IRepository<FileEntity, FileId> fileRepository, WallpaperFileRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var wallpaper = request.Wallpaper;
        var fileEntity = new FileEntity
        {
            Id = FileId.Empty,
            FileName = new FileName($"{wallpaper.Id}{request.Extension}"),
            DirectoryName = DirectoryName.Create(request.Directory),
            FileAccessDetail = new FileAccessDetailEntity
            {
                DetailsLastUpdated = clock().UtcDateTime,
                Id = FileAccessDetailId.Empty,
                FileId = FileId.Empty
            },
            FileSize = wallpaper.FileSize,
            FileHandle = FileHandle.Create(wallpaper.Id),
            FileType = wallpaper.FileType,
            IsImage = wallpaper.Path.IsImage,
            ImageDetail = new ImageDetailEntity
            {
                Id = ImageId.Empty,
                FileId = FileId.Empty,
                Width = wallpaper.DimensionX,
                Height = wallpaper.DimensionY,
            }
        };

        return Task.FromResult(fileRepository.Add(fileEntity));
    }
}
