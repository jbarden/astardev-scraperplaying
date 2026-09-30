using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class PagesProcessor(IHttpClientFactory httpClientFactory, IUnitOfWork unitOfWork, IJsonResponseProcessor jsonResponseProcessor, ISaveDirectoryResolver saveDirectoryResolver, IWallpaperIngestionService wallpaperIngestionService) : IPagesProcessor
{
    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Func<int, Uri> pageUrlFactory, WallhavenConnection connection, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var client = CreateHttpClient(connection);
        try
        {
            var page = 1;
            var fileRepository = unitOfWork.GetRepository<FileEntity, FileId>();
            var directory = await saveDirectoryResolver.ResolveSaveDirectoryAsync(categoryName, cancellationToken);
            var categoryLabel = categoryName.Match(name => name, () => "Top Wallpapers");
            var ingestionContext = new WallpaperIngestionContext(directory, client, fileRepository, categoryLabel, personCategories);
            SearchResponse pageResult;
            do
            {
                pageResult = await FetchPageAsync(logLabel, pageUrlFactory, page, client, progress, cancellationToken);

                await wallpaperIngestionService.IngestPageAsync(pageResult.Data, ingestionContext, progress, cancellationToken);

                _ = await unitOfWork.SaveChangesAsync(cancellationToken);
                page++;
            } while (page <= pageResult.Meta.LastPage && page <= 4);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await SavePartiallyIngestedPageAsync(progress);

            throw;
        }
        catch (Exception ex)
        {
            progress.Report($"An error occurred during the fetching and processing of pages: {ex.Message}");
            throw;
        }
    }

    private async Task SavePartiallyIngestedPageAsync(IProgress<string> progress)
    {
        try
        {
            _ = await unitOfWork.SaveChangesAsync(CancellationToken.None);
            progress.Report("Scrape cancelled - saved wallpapers downloaded so far this page.");
        }
        catch (DbUpdateException ex)
        {
            progress.Report($"Scrape cancelled - failed to save wallpapers downloaded so far this page: {ex.Message}");
        }
    }

    private async Task<SearchResponse> FetchPageAsync(string logLabel, Func<int, Uri> pageUrlFactory, int page, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
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

