using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public class PagesProcessor(IHttpClientFactory httpClientFactory, IUnitOfWork unitOfWork, IFilesQuery filesQuery, IJsonResponseProcessor jsonResponseProcessor, IImageProcessor imageProcessor) : IPagesProcessor
{
    /// <summary>The named <see cref="HttpClient"/> configured with the scraper's static headers via <c>AddHttpClient</c>.</summary>
    public const string HttpClientName = "Wallhaven";


    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(string logLabel, Func<int, string> pageUrlFactory, string apiKey, string sessionCookie, Uri baseUrl, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var client = CreateHttpClient(apiKey, sessionCookie, baseUrl);
        try
        {
            var page = 1;
            var fileRepository = unitOfWork.GetRepository<FileEntity, FileId>();
            SearchResponse pageResult;
            await Task.Delay(2_000, cancellationToken);
            do
            {
                progress.Report($"Fetching {logLabel} page {page}.");
                pageResult = (await jsonResponseProcessor.GetFromJsonAsync<SearchResponse>(pageUrlFactory(page), client, cancellationToken))!;
                await Task.Delay(2_000, cancellationToken);
                foreach (var wallpaper in pageResult.Data)
                {
                    _ = (await filesQuery.CheckExistsByNameAsync(new FileName(wallpaper.Id), cancellationToken))
                    .Match(
                        async notFound =>
                        {
                            await Try.RunAsync(async () =>
                            {
                                progress.Report($"No existing file found for wallpaper {wallpaper.Id}.");
                                await imageProcessor.DownloadImageAsync(wallpaper.Id, wallpaper.Path, progress, client, cancellationToken);
                                progress.Report($"Downloaded image data for wallpaper {wallpaper.Id}");
                                await Task.Delay(2_000, cancellationToken);
                                await imageProcessor.ProcessTheImageAsync(progress, fileRepository, wallpaper, cancellationToken);

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
                         async _ =>
                         {
                             progress.Report($"The file details already exist for wallpaper {wallpaper.Id} - no need to fetch again.");
                         }
                    );
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await Task.Delay(2_000, cancellationToken);
                page++;
            } while (page <= pageResult.Meta.LastPage && page <= 4);
        }
        catch (Exception ex)
        {
            progress.Report($"An error occurred during the fetching and processing of pages: {ex.Message}");
            throw;
        }
    }

    private HttpClient CreateHttpClient(string apiKey, string sessionCookie, Uri baseUrl)
    {
        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = baseUrl;
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        if (!string.IsNullOrWhiteSpace(sessionCookie)) httpClient.DefaultRequestHeaders.Add("Cookie", sessionCookie);

        httpClient.DefaultRequestHeaders.Referrer = baseUrl;

        return httpClient;
    }
}

