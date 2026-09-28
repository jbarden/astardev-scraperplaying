using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class WallpaperIngestionService(IFilesQuery filesQuery, IImageProcessor imageProcessor, ITagsProcessor tagsProcessor, Func<TimeSpan> pacingDelay) : IWallpaperIngestionService
{
    /// <inheritdoc/>
    public async Task IngestAsync(Data wallpaper, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
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
                    progress.Report($"No existing data found for wallpaper {wallpaper.Id}.");
                    var tags = await FetchTagsAsync(wallpaper.Id, context.Client, progress, cancellationToken);
                    var fileRequest = new WallpaperFileRequest(wallpaper, context.Directory, WallpaperFileNamer.Create(wallpaper.Id, extension, tags), context.CategoryLabel);

                    await imageProcessor.DownloadImageAsync(fileRequest, progress, context.Client, cancellationToken);
                    progress.Report($"Downloaded image data for wallpaper {wallpaper.Id}");
                    await Task.Delay(pacingDelay(), cancellationToken);

                    var fileEntity = (await imageProcessor.ProcessTheImageAsync(context.FileRepository, fileRequest, cancellationToken))
                        .Match(entity => entity, ex => throw ex);

                    await tagsProcessor.LinkTagsAsync(fileEntity.Id, tags, cancellationToken)
                        .MatchAsync(
                            _ => Task.CompletedTask,
                            ex =>
                            {
                                progress.Report($"Failed to link tags for wallpaper {wallpaper.Id}: {ex.Message}");

                                return Unit.Instance;
                            }
                        );

                    return Unit.Instance;
                }).MatchAsync(
                    _ => Task.CompletedTask,
                    y =>
                    {
                        progress.Report($"Failed to process image for wallpaper {wallpaper.Id}: {y.Message}");

                        return Unit.Instance;
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

    private async Task<IReadOnlyList<Tag>> FetchTagsAsync(string wallpaperId, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
        await Task.Delay(pacingDelay(), cancellationToken);

        return (await tagsProcessor.FetchTagsAsync(wallpaperId, client, progress, cancellationToken))
            .Match(
                tags => tags,
                ex =>
                {
                    progress.Report($"Failed to fetch tags for wallpaper {wallpaperId}: {ex.Message}");

                    return [];
                });
    }
}
