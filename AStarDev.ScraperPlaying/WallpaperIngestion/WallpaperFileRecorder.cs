using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class WallpaperFileRecorder(Func<DateTimeOffset> clock) : IWallpaperFileRecorder
{
    /// <inheritdoc/>
    public Exceptional<FileEntity> Record(WallpaperDetail detail, DownloadedWallpaperImage image, string directory, IRepository<FileEntity, FileId> fileRepository)
        => fileRepository.Add(new FileEntity
        {
            Id = FileId.Empty,
            FileName = image.FileName,
            DirectoryName = DirectoryName.Create(directory),
            FileAccessDetail = new FileAccessDetailEntity
            {
                DetailsLastUpdated = clock().UtcDateTime,
                Id = FileAccessDetailId.Empty,
                FileId = FileId.Empty
            },
            FileSize = image.SizeBytes,
            FileHandle = FileHandle.Create(detail.WallpaperId),
            FileType = image.ContentType,
            IsImage = detail.ImageUrl.IsImage,
            ImageDetail = new ImageDetailEntity
            {
                Id = ImageId.Empty,
                FileId = FileId.Empty,
                Width = detail.DimensionX,
                Height = detail.DimensionY
            }
        });
}
