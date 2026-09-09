using System.Diagnostics;
using System.Net.Http.Headers;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using System.Net.Http.Json;
using System.Text.Json;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.DetailResponse;
using AStarDev.Utilities;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb;

namespace AStarDev.ScraperPlaying.Home;

public class ScrapeService(OperationCoordinator operationCoordinator, IUnitOfWork unitOfWork) : IScrapeService
{
    private static readonly HttpClient client = CreateHttpClient();
    public async Task RunScraperAsync(IProgress<string> progress)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        var startTime = Stopwatch.GetTimestamp();
        try
        {
            progress.Report("Starting scrape operation.");
            ScrapeConfigurationEntity configuration = (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
            .Match(
                scrapeConfigurationEntity => scrapeConfigurationEntity.Match<ScrapeConfigurationEntity>(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                _ => {
                    progress.Report("Scrape configuration not found");
                    return null!;
                }
            )!;

            var apiKey = configuration.UserConfiguration.ApiKey;
            var sessionCookie = configuration.UserConfiguration.SessionCookie;
            var encodedApiKey = Uri.EscapeDataString(apiKey);
            var topWallpapersUrl = configuration.SearchConfiguration.TopWallpapers.Replace("%7BapiKey%7D", encodedApiKey);
            var searchCategoriesUrl = configuration.SearchConfiguration.SearchStringPrefix.Replace("%7BapiKey%7D", encodedApiKey);
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

    private static async Task<T?> GetFromJsonAsync<T>(string url, string? sessionCookie, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var cookieHeader = NormalizeCookieHeader(sessionCookie);
        if (cookieHeader is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

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

    private static async Task FetchAndProcessPagesAsync(string logLabel, Func<int, string> pageUrlFactory, Func<int, string> pageFileNameFactory, string sessionCookie, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var page = 1;
        SearchResponse pageResult;
        do
        {
            progress.Report($"Fetching {logLabel} page {page}.");
            pageResult = (await GetFromJsonAsync<SearchResponse>(pageUrlFactory(page), sessionCookie, cancellationToken))!;
            await File.WriteAllTextAsync(pageFileNameFactory(page), pageResult.ToJson(), cancellationToken);
            await Task.Delay(1000, cancellationToken);
            foreach (var wallpaper in pageResult.Data)
            {
                await GetImageDetails(sessionCookie, wallpaper.Id, progress, cancellationToken);
            }
            page++;
        } while (page <= pageResult.Meta.LastPage && page <= 4);
    }

    private static async Task GetImageDetails(string sessionCookie, string wallpaperId, IProgress<string> progress, CancellationToken cancellationToken)
    {
        string detailUrl = $"{BaseUrl}/w/{wallpaperId}";
        progress.Report($"Fetching details for wallpaper {wallpaperId}.");
        var detailResponse = await GetFromJsonAsync<DetailResponse>(detailUrl, sessionCookie, cancellationToken);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        progress.Report($"Detail response for wallpaper {wallpaperId}: {detailResponse.Data}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        await Task.Delay(1000, cancellationToken);

        await GetTags(wallpaperId, detailResponse, progress, cancellationToken);

        var imageResponse = await client.GetAsync(detailResponse.Data.Path, cancellationToken);
        imageResponse.EnsureSuccessStatusCode();
        progress.Report($"Fetched image for wallpaper {wallpaperId}.");
        var imageData = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        progress.Report($"Downloaded image data for wallpaper {wallpaperId}, size: {imageData.Length} bytes.");
        SaveImageData(wallpaperId, imageData, progress);
    }

    private static void SaveImageData(string wallpaperId, byte[] imageData, IProgress<string> progress)
    {
        progress.Report($"Saving image data for wallpaper {wallpaperId}, size: {imageData.Length} bytes.");
        File.WriteAllBytes($"{wallpaperId}.jpg", imageData);
    }

    private static async Task GetTags(string wallpaperId, DetailResponse detailResponse, IProgress<string> progress, CancellationToken cancellationToken)
    {
        foreach (var tag in detailResponse.Data.Tags)
        {
            progress.Report($"Tag for wallpaper {wallpaperId}: {tag}");
            await Task.Delay(100, cancellationToken);
        }
    }
    private const string BaseUrl = "https://wallhaven.cc/api/v1";

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
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