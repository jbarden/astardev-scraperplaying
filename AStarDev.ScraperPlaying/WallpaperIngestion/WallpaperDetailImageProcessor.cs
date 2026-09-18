using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.Utilities;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class WallpaperDetailImageProcessor(IFilesQuery filesQuery, IFileSystem fileSystem, Func<DateTimeOffset> clock, IImageDownloadNotifier imageDownloadNotifier) : IWallpaperDetailImageProcessor
{
    /// <inheritdoc/>
    public Task<Exceptional<Option<FileEntity>>> DownloadAndRecordAsync(WallpaperDetail detail, IPage page, string directory, string categoryLabel, IRepository<FileEntity, FileId> fileRepository, IProgress<string> progress, CancellationToken cancellationToken)
        => Try.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(detail.ImageUrl)) throw new InvalidOperationException($"No image URL was found on the detail page for wallpaper {detail.WallpaperId}.");

            var extension = detail.ImageUrl.ToFileExtension();
            var fileName = new FileName($"{detail.WallpaperId}{extension}");

            var exists = (await filesQuery.CheckExistsByNameAsync(fileName, cancellationToken)).Match(value => value, ex => throw ex);
            if (exists)
            {
                progress.Report($"The file details already exist for wallpaper {detail.WallpaperId} - no need to fetch again.");

                return Option.None<FileEntity>();
            }

            progress.Report($"Downloading image for wallpaper {detail.WallpaperId} from {detail.ImageUrl}");
            var response = await page.Context.APIRequest.GetAsync(detail.ImageUrl).WaitAsync(cancellationToken);
            if (!response.Ok) throw new HttpRequestException($"Downloading the image for wallpaper {detail.WallpaperId} failed with status {response.Status}.");

            var body = await response.BodyAsync().WaitAsync(cancellationToken);
            fileSystem.Directory.CreateDirectory(directory);
            var savedPath = fileSystem.Path.Combine(directory, fileName.Value);
            await fileSystem.File.WriteAllBytesAsync(savedPath, body, cancellationToken);

            var fileEntity = new FileEntity
            {
                Id = FileId.Empty,
                FileName = fileName,
                DirectoryName = DirectoryName.Create(directory),
                FileAccessDetail = new FileAccessDetailEntity
                {
                    DetailsLastUpdated = clock().UtcDateTime,
                    Id = FileAccessDetailId.Empty,
                    FileId = FileId.Empty
                },
                FileSize = body.Length,
                FileHandle = FileHandle.Create(detail.WallpaperId),
                FileType = response.Headers.GetValueOrDefault("content-type", string.Empty),
                IsImage = detail.ImageUrl.IsImage,
                ImageDetail = new ImageDetailEntity
                {
                    Id = ImageId.Empty,
                    FileId = FileId.Empty,
                    Width = detail.DimensionX,
                    Height = detail.DimensionY
                }
            };

            var added = fileRepository.Add(fileEntity).Match(entity => entity, ex => throw ex);
            imageDownloadNotifier.NotifyImageDownloaded(new WallpaperDownloadDetails(savedPath, detail.WallpaperId, categoryLabel, body.Length, detail.DimensionX, detail.DimensionY));

            return Option.Some(added);
        }, cancellationToken);
}
