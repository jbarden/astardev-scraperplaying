using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class WallpaperIngestionContextFactory(IWallhavenClientFactory clientFactory, IUnitOfWork unitOfWork, ISaveDirectoryResolver saveDirectoryResolver) : IWallpaperIngestionContextFactory
{
    /// <inheritdoc/>
    public async Task<WallpaperIngestionContext> CreateAsync(PageScrapeRequest request, CancellationToken cancellationToken)
    {
        var client = clientFactory.Create(request.Target.Connection);
        var directories = await saveDirectoryResolver.ResolveSaveDirectoriesAsync(request.Label.CategoryName, cancellationToken);
        var categoryLabel = request.Label.CategoryName.Match(name => name, () => "Top Wallpapers");

        return new WallpaperIngestionContext(directories, client, unitOfWork.GetRepository<FileEntity, FileId>(), categoryLabel, request.Target.PersonCategories);
    }
}
