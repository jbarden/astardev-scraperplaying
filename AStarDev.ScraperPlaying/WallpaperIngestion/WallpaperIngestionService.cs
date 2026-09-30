using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class WallpaperIngestionService(IFilesQuery filesQuery, IImageProcessor imageProcessor, ITagsProcessor tagsProcessor) : IWallpaperIngestionService
{
    /// <inheritdoc/>
    public async Task IngestPageAsync(IReadOnlyList<Data> wallpapers, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (wallpapers.Count == 0) return;

        IReadOnlyList<(Data Wallpaper, string Extension)> candidates = [.. wallpapers.Select(wallpaper => (wallpaper, wallpaper.Path.ToFileExtension()))];
        var names = candidates.Select(candidate => new FileName($"{candidate.Wallpaper.Id}{candidate.Extension}")).ToList();

        await (await filesQuery.GetExistingNamesAsync(names, cancellationToken))
        .Match(
            async existingNames =>
            {
                var existing = existingNames.Select(name => name.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var (wallpaper, extension) in candidates)
                {
                    if (existing.Contains($"{wallpaper.Id}{extension}"))
                    {
                        progress.Report($"The file details already exist for wallpaper {wallpaper.Id} - no need to fetch again.");

                        continue;
                    }

                    await IngestNewWallpaperAsync(wallpaper, extension, context, progress, cancellationToken);
                }
            },
            exception =>
            {
                progress.Report($"Failed to check whether the file details already exist for this page of wallpapers: {exception.Message}");

                return Task.CompletedTask;
            }
        );
    }

    private Task IngestNewWallpaperAsync(Data wallpaper, string extension, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
        => Try.RunAsync(async () =>
        {
            progress.Report($"No existing data found for wallpaper {wallpaper.Id}.");
            var tags = await FetchTagsAsync(wallpaper.Id, context.Client, progress, cancellationToken);
            var fileRequest = new WallpaperFileRequest(wallpaper, context.Directory, WallpaperFileNamer.Create(wallpaper.Id, extension, tags, context.PersonCategories), context.CategoryLabel);

            await imageProcessor.DownloadImageAsync(fileRequest, progress, context.Client, cancellationToken);
            progress.Report($"Downloaded image data for wallpaper {wallpaper.Id}");

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
            exception =>
            {
                progress.Report($"Failed to process image for wallpaper {wallpaper.Id}: {exception.Message}");

                return Unit.Instance;
            }
        );

    private async Task<IReadOnlyList<Tag>> FetchTagsAsync(string wallpaperId, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
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
