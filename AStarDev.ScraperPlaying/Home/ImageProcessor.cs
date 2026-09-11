using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public class ImageProcessor(Func<DateTimeOffset> clock, IUnitOfWork unitOfWork) : IImageProcessor
{
    /// <inheritdoc/>
    public async Task DownloadImageAsync(string id, string path, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
    {
        await Task.Delay(2_000, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        progress.Report($"Downloading image for wallpaper {id} from {path}");
        using Stream downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using FileStream fileStream = new($"{id}.jpg", FileMode.Create, FileAccess.Write, FileShare.None);

        await downloadStream.CopyToAsync(fileStream, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ProcessTheImageAsync(IProgress<string> progress, IRepository<FileEntity, FileId> fileRepository, Data wallpaper, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = new FileName(wallpaper.Id);
            var fileEntity = new FileEntity
            {
                Id = FileId.Empty,
                FileName = fileName,
                DirectoryName = DirectoryName.Create("TBC"),
                FileAccessDetail = new FileAccessDetailEntity
                {
                    DetailsLastUpdated = clock().UtcDateTime,
                    Id = FileAccessDetailId.Empty,
                    FileId = FileId.Empty
                },
                FileSize = wallpaper.FileSize,
                FileHandle = FileHandle.Create(wallpaper.Id),
                FileType = wallpaper.FileType,
                ImageDetail = new ImageDetailEntity
                {
                    Id = ImageId.Empty,
                    FileId = FileId.Empty,
                    Width = wallpaper.DimensionX,
                    Height = wallpaper.DimensionY,
                }
            };

            fileRepository.Add(fileEntity);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            progress.Report($"Failed to process image for wallpaper {wallpaper.Id}: {ex.Message}");
            throw;
        }
    }
}
