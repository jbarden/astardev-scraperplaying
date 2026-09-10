using System.Diagnostics;
using System.Net.Http.Headers;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using System.Net.Http.Json;
using System.Text.Json;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ScraperPlaying.Home;

public class ScrapeService(OperationCoordinator operationCoordinator, IUnitOfWork unitOfWork, IFilesQuery filesQuery, Func<DateTimeOffset> clock) : IScrapeService
{
        private const string BaseUrl = "https://wallhaven.cc/";
        private static HttpClient client = default!;

    public async Task RunScraperAsync(IProgress<string> progress)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        var startTime = Stopwatch.GetTimestamp();
        try
        {
            progress.Report("Starting scrape operation.");
            ScrapeConfigurationEntity configuration = (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
                .Match(scrapeConfigurationEntity => scrapeConfigurationEntity
                    .Match(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                    _ =>
                    {
                        progress.Report("Scrape configuration not found");
                        return null!;
                    }
            )!;

            var apiKey = configuration.UserConfiguration.ApiKey;
            client = CreateHttpClient(apiKey);
            var sessionCookie = configuration.UserConfiguration.SessionCookie;
            var encodedApiKey = Uri.EscapeDataString(apiKey);
            var topWallpapersUrl = configuration.SearchConfiguration.TopWallpapers.Replace("apiKey={apiKey}&", string.Empty);
            var searchCategoriesUrl = configuration.SearchConfiguration.SearchStringPrefix.Replace("apiKey={apiKey}&", string.Empty);
            var searchCategories = configuration.SearchConfiguration.SearchCategories;

            progress.Report("Fetching top wallpapers.");
            await FetchAndProcessPagesAsync(
                "top wallpapers",
                page => topWallpapersUrl + page,
                page => page == 1 ? "topWallpapers-1.json" : $"topWallpapers-{page}.json",
                sessionCookie,
                progress,
                cancellationToken);

            foreach (var category in searchCategories.Take(3))
            {
                await FetchAndProcessPagesAsync(
                    $"search category {category.Id}",
                    page => page == 1
                        ? searchCategoriesUrl.Replace("%7Bid%7D", category.Id)
                        : searchCategoriesUrl.Replace("%7Bid%7D", category.Id) + page,
                    page => page == 1 ? $"{category.Id}.json" : $"{category.Id}-{page}.json",
                    sessionCookie,
                    progress,
                    cancellationToken);
            }

            progress.Report($"Search completed in: {Stopwatch.GetElapsedTime(startTime).TotalMilliseconds} total milliseconds.");
        }
        catch (HttpRequestException e)
        {
            progress.Report($"Request error: {e.Message}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            progress.Report("Search cancelled.");
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    private static async Task<T?> GetFromJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Wallhaven returned {(int)response.StatusCode} ({response.StatusCode}) for {url}. " +
                $"Location: {response.Headers.Location}. Response: {responseBody}");
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new InvalidOperationException($"Unable to deserialize response from {url} as {typeof(T).Name}.", exception);
        }
    }

    private async Task FetchAndProcessPagesAsync(string logLabel, Func<int, string> pageUrlFactory, Func<int, string> pageFileNameFactory, string sessionCookie, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            var page = 1;
            var fileRepository = unitOfWork.GetRepository<FileEntity, FileId>();
            SearchResponse pageResult;
            await Task.Delay(2_000, cancellationToken);
            do
            {
                progress.Report($"Fetching {logLabel} page {page}.");
                pageResult = (await GetFromJsonAsync<SearchResponse>(pageUrlFactory(page), cancellationToken))!;
                await File.WriteAllTextAsync(pageFileNameFactory(page), pageResult.ToJson(), cancellationToken);
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
                                await ProcessTheImageAsync(progress, fileRepository, wallpaper, cancellationToken);
                                await Task.Delay(2_000, cancellationToken);
                                await DownloadImageAsync(wallpaper.Id, wallpaper.Path, progress, cancellationToken);
                                progress.Report($"Downloaded image data for wallpaper {wallpaper.Id}");
                                // SaveImageData(wallpaper.Id, imageData, "TBC", progress);
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

                await Task.Delay(2_000, cancellationToken);
                page++;
            } while (page <= pageResult.Meta.LastPage && page <= 4);
        }
        catch (System.Exception ex)
        {
            progress.Report($"An error occurred during the fetching and processing of pages: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessTheImageAsync(IProgress<string> progress, IRepository<FileEntity, FileId> fileRepository, SearchAPI.SearchResponse.Data wallpaper, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = new FileName(wallpaper.Id);
            var fileEntity = new FileEntity
            {
                Id = FileId.Empty,
                FileName = fileName,
                DirectoryName = DirectoryName.Create("TBC"),
                FileAccessDetail = new FileAccessDetailEntity
                {
                    DetailsLastUpdated = clock().UtcDateTime,
                    Id = FileAccessDetailId.Empty,
                    FileId = FileId.Empty
                },
                FileSize = wallpaper.FileSize,
                FileHandle = FileHandle.Create(wallpaper.Id),
                FileType = wallpaper.FileType,
                ImageDetail = new ImageDetailEntity
                {
                    Id = ImageId.Empty,
                    FileId = FileId.Empty,
                    Width = wallpaper.DimensionX,
                    Height = wallpaper.DimensionY,
                }
            };

            // fileRepository.Add(fileEntity);
            // await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (System.Exception ex)
        {
            progress.Report($"Failed to process image for wallpaper {wallpaper.Id}: {ex.Message}");
            throw;
        }
    }

    private static async Task DownloadImageAsync(string wallpaperId, string imageUri, IProgress<string> progress, CancellationToken cancellationToken)
    {
        await Task.Delay(2_000, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, imageUri);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        progress.Report($"Downloading image for wallpaper {wallpaperId} from {imageUri}");
        using Stream downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using FileStream fileStream = new($"{wallpaperId}.jpg", FileMode.Create, FileAccess.Write, FileShare.None);

        await downloadStream.CopyToAsync(fileStream, cancellationToken);
    }

    private static HttpClient CreateHttpClient(string apiKey)
    {
        var httpClient = new HttpClient() { BaseAddress = new Uri(BaseUrl) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        httpClient.DefaultRequestHeaders.Referrer = new Uri(BaseUrl);
        return httpClient;
    }

    private static string? NormalizeCookieHeader(string? sessionCookie)
    {
        if (string.IsNullOrWhiteSpace(sessionCookie)) return null;

        var cookieHeader = sessionCookie.Trim();
        if (cookieHeader.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
        {
            cookieHeader = cookieHeader["Cookie:".Length..].Trim();
        }

        return cookieHeader;
    }
}