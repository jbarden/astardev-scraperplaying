using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public class PagesProcessor(IHttpClientFactory httpClientFactory, IUnitOfWork unitOfWork, IFilesQuery filesQuery, IJsonResponseProcessor jsonResponseProcessor, IImageProcessor imageProcessor, ITagsProcessor tagsProcessor, Func<TimeSpan> pacingDelay) : IPagesProcessor
{
    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(string logLabel, Func<int, string> pageUrlFactory, string apiKey, Uri baseUrl, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var client = CreateHttpClient(apiKey, baseUrl);
        try
        {
            var page = 1;
            var fileRepository = unitOfWork.GetRepository<FileEntity, FileId>();
            SearchResponse pageResult;
            await Task.Delay(pacingDelay(), cancellationToken);
            do
            {
                pageResult = await FetchPageAsync(logLabel, pageUrlFactory, page, client, progress, cancellationToken);
                await Task.Delay(pacingDelay(), cancellationToken);

                foreach (var wallpaper in pageResult.Data)
                {
                    await ProcessWallpaperAsync(wallpaper, client, fileRepository, progress, cancellationToken);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await Task.Delay(pacingDelay(), cancellationToken);
                page++;
            } while (page <= pageResult.Meta.LastPage && page <= 4);
        }
        catch (Exception ex)
        {
            progress.Report($"An error occurred during the fetching and processing of pages: {ex.Message}");
            throw;
        }
    }

    private async Task<SearchResponse> FetchPageAsync(string logLabel, Func<int, string> pageUrlFactory, int page, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"Fetching {logLabel} page {page}.");

        return (await jsonResponseProcessor.GetFromJsonAsync<SearchResponse>(pageUrlFactory(page), client, cancellationToken))
            .Match(
                option => option.Match(value => value, () => throw new InvalidOperationException($"No response body received for {pageUrlFactory(page)}.")),
                exception => throw exception);
    }

    private async Task ProcessWallpaperAsync(Data wallpaper, HttpClient client, IRepository<FileEntity, FileId> fileRepository, IProgress<string> progress, CancellationToken cancellationToken)
    {
        await (await filesQuery.CheckExistsByNameAsync(new FileName(wallpaper.Id), cancellationToken))
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
                    await imageProcessor.DownloadImageAsync(wallpaper.Id, wallpaper.Path, progress, client, cancellationToken);
                    progress.Report($"Downloaded image data for wallpaper {wallpaper.Id}");
                    await Task.Delay(pacingDelay(), cancellationToken);

                    var fileEntity = (await imageProcessor.ProcessTheImageAsync(fileRepository, wallpaper, cancellationToken))
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

    private HttpClient CreateHttpClient(string apiKey, Uri baseUrl)
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.WallhavenHttpClientName);
        httpClient.BaseAddress = baseUrl;
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        httpClient.DefaultRequestHeaders.Referrer = baseUrl;

        return httpClient;
    }
}

