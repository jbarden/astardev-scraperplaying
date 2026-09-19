using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Interface for recording a downloaded wallpaper image as a <see cref="FileEntity"/>.</summary>
public interface IWallpaperFileRecorder
{
    /// <summary>Builds the <see cref="FileEntity"/> (with its access and image details) for a downloaded wallpaper and adds it to the repository.</summary>
    /// <param name="detail">The wallpaper scraped from its detail page.</param>
    /// <param name="image">The downloaded image.</param>
    /// <param name="directory">The directory the image was saved into.</param>
    /// <param name="fileRepository">The repository used to store the file entity.</param>
    /// <returns>An <see cref="Exceptional{T}"/> wrapping the added entity, or the captured failure.</returns>
    Exceptional<FileEntity> Record(WallpaperDetail detail, DownloadedWallpaperImage image, string directory, IRepository<FileEntity, FileId> fileRepository);
}
