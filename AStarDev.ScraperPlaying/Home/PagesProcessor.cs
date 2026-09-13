using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public class PagesProcessor(IHttpClientFactory httpClientFactory, IUnitOfWork unitOfWork, IJsonResponseProcessor jsonResponseProcessor, ISaveDirectoryResolver saveDirectoryResolver, IWallpaperIngestionService wallpaperIngestionService, Func<TimeSpan> pacingDelay) : IPagesProcessor
{
    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Func<int, string> pageUrlFactory, WallhavenConnection connection, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var client = CreateHttpClient(connection);
        try
        {
            var page = 1;
            var fileRepository = unitOfWork.GetRepository<FileEntity, FileId>();
            var directory = await saveDirectoryResolver.ResolveSaveDirectoryAsync(categoryName, cancellationToken);
            var ingestionContext = new WallpaperIngestionContext(directory, client, fileRepository);
            SearchResponse pageResult;
            await Task.Delay(pacingDelay(), cancellationToken);
            do
            {
                pageResult = await FetchPageAsync(logLabel, pageUrlFactory, page, client, progress, cancellationToken);
                await Task.Delay(pacingDelay(), cancellationToken);

                foreach (var wallpaper in pageResult.Data)
                {
                    await wallpaperIngestionService.IngestAsync(wallpaper, ingestionContext, progress, cancellationToken);
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

    private HttpClient CreateHttpClient(WallhavenConnection connection)
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.WallhavenHttpClientName);
        httpClient.BaseAddress = connection.BaseUrl;
        httpClient.DefaultRequestHeaders.Add("X-API-Key", connection.ApiKey);
        httpClient.DefaultRequestHeaders.Referrer = connection.BaseUrl;

        return httpClient;
    }
}

