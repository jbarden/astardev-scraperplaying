using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public class WallpaperIngestionService(IFilesQuery filesQuery, IImageProcessor imageProcessor, ITagsProcessor tagsProcessor, Func<TimeSpan> pacingDelay) : IWallpaperIngestionService
{
    /// <inheritdoc/>
    public async Task IngestAsync(Data wallpaper, string directory, HttpClient client, IRepository<FileEntity, FileId> fileRepository, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var extension = wallpaper.Path.ToFileExtension();

        await (await filesQuery.CheckExistsByNameAsync(new FileName($"{wallpaper.Id}{extension}"), cancellationToken))
        .Match(
            async exists =>
            {
                if (exists)
                {
                    progress.Report($"The file details already exist for wallpaper {wallpaper.Id} - no need to fetch again.");

                    return;
                }

                await Try.RunAsync(async () =>
                {
                    progress.Report($"No existing file found for wallpaper {wallpaper.Id}.");
                    await imageProcessor.DownloadImageAsync(wallpaper.Id, wallpaper.Path, extension, directory, progress, client, cancellationToken);
                    progress.Report($"Downloaded image data for wallpaper {wallpaper.Id}");
                    await Task.Delay(pacingDelay(), cancellationToken);

                    var fileEntity = (await imageProcessor.ProcessTheImageAsync(fileRepository, wallpaper, directory, extension, cancellationToken))
                        .Match(entity => entity, ex => throw ex);

                    await Task.Delay(pacingDelay(), cancellationToken);
                    await tagsProcessor.FetchAndLinkTagsAsync(wallpaper.Id, fileEntity.Id, client, progress, cancellationToken)
                        .MatchAsync(
                            _ => Task.CompletedTask,
                            ex =>
                            {
                                progress.Report($"Failed to fetch tags for wallpaper {wallpaper.Id}: {ex.Message}");

                                return UnitFp.Instance;
                            }
                        );

                    return UnitFp.Instance;
                }).MatchAsync(
                    _ => Task.CompletedTask,
                    y =>
                    {
                        progress.Report($"Failed to process image for wallpaper {wallpaper.Id}: {y.Message}");

                        return UnitFp.Instance;
                    }
                );
            },
            exception =>
            {
                progress.Report($"Failed to check whether the file details already exist for wallpaper {wallpaper.Id}: {exception.Message}");

                return Task.CompletedTask;
            }
        );
    }
}
