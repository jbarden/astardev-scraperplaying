using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using AStar.Dev.Logging.Extensions;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI;
using AStarDev.ScraperPlaying.SearchAPI.DetailResponse;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using AStarDev.ScraperPlaying.SearchAPI.TagResponse;
using AStarDev.Utilities;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace AStarDev.ScraperPlaying.Home;

public partial class MainWindow : Window
{
    private const int MaximumStatusMessages = 100;
    private static readonly HttpClient client = CreateHttpClient();
    private readonly Queue<string> statusMessages = new();
    private readonly IScrapeConfigurationRepository scrapeConfigurationRepository;
    private readonly IScrapeConfigurationImportService importService;
    private readonly IConfigurationFilePicker configurationFilePicker;
    private readonly ILogger<MainWindow> logger;

    // Replace with your actual Wallhaven API key if needed SOME_FAKE_API_KEY_AS_PLACEHOLDER
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

    private static async Task<T?> GetFromJsonAsync<T>(string url, string? sessionCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var cookieHeader = NormalizeCookieHeader(sessionCookie);
        if (cookieHeader is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Wallhaven returned {(int)response.StatusCode} ({response.StatusCode}) for {url}. " +
                $"Location: {response.Headers.Location}. Response: {responseBody}");
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new InvalidOperationException($"Unable to deserialize response from {url} as {typeof(T).Name}.", exception);
        }
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

    public MainWindow(
        ILogger<MainWindow> logger,
        IScrapeConfigurationRepository scrapeConfigurationRepository,
        IScrapeConfigurationImportService importService,
        IConfigurationFilePicker configurationFilePicker)
    {
        InitializeComponent();
        this.scrapeConfigurationRepository = scrapeConfigurationRepository;
        this.importService = importService;
        this.configurationFilePicker = configurationFilePicker;
        this.logger = logger;
    }

    public static MainWindow CreateStartupError(Exception exception)
    {
        var window = new MainWindow(NullLogger<MainWindow>.Instance, null!, null!, null!);
        window.AppendStatusMessage($"Startup failed: {exception.GetType().Name}: {exception.Message}");
        return window;
    }

    public async void ImportConfiguration(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            var filePath = await configurationFilePicker.PickAsync(this);
            if (filePath is null) return;

            await importService.ImportAsync(filePath);
            StatusTextBlock.Text = "Scrape configuration imported.";
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException)
        {
            LogError("Unable to import scrape configuration.", exception);
        }
    }

    public async void RunScraper(object? sender, RoutedEventArgs eventArgs)
    {
        var startTime = Stopwatch.GetTimestamp();
        try
        {
            LogInformation("Starting scrape operation.");
            var configuration = (await scrapeConfigurationRepository.GetScrapeConfigurationAsync())
            .Match(
                c => c,
                _ => throw new InvalidOperationException("Scrape configuration not found")
            )!;

            var apiKey = configuration.UserConfiguration.ApiKey;
            var sessionCookie = configuration.UserConfiguration.SessionCookie;
            var encodedApiKey = Uri.EscapeDataString(apiKey);
            var topWallpapersUrl = configuration.TopWallpapersUrl.AbsoluteUri.Replace("%7BapiKey%7D", encodedApiKey);
            var searchCategoriesUrl = configuration.SearchCategoriesUrl.AbsoluteUri.Replace("%7BapiKey%7D", encodedApiKey);
            var searchCategories = configuration.SearchCategories;

            LogInformation("Fetching top wallpapers.");
            var topWallpapersResult = await GetFromJsonAsync<SearchResponse>(topWallpapersUrl + 1, sessionCookie);
            await File.WriteAllTextAsync("topWallpapers-1.json", topWallpapersResult.ToJson());

            foreach (var wallpaper in topWallpapersResult!.Data)
            {
                await GetImageDetails(sessionCookie, wallpaper.Id);
            }

            for (var i = 2; i <= topWallpapersResult!.Meta.LastPage; i++)
            {
                var pageUrl = configuration.TopWallpapersUrl.AbsoluteUri + i;
#pragma warning disable CA1873 // Avoid potentially expensive logging
                LogInformation($"Fetching top wallpapers page {i}.");
#pragma warning restore CA1873 // Avoid potentially expensive logging
                var pageResult = await GetFromJsonAsync<SearchResponse>(pageUrl, sessionCookie);
                await File.WriteAllTextAsync($"topWallpapers-{i}.json", pageResult.ToJson());
                await Task.Delay(1000); // Add a small delay to avoid overwhelming the server
                foreach (var wallpaper in pageResult!.Data)
                {
                    await GetImageDetails(sessionCookie, wallpaper.Id);
                }
                // You can process pageResult here as needed
                if (i == 4)
                    break;
            }

            foreach (var category in searchCategories.Take(3))
            {
#pragma warning disable CA1873 // Avoid potentially expensive logging
                LogInformation($"Fetching search category {category.Id} page 1.");
#pragma warning restore CA1873 // Avoid potentially expensive logging
                var searchResponse = await GetFromJsonAsync<SearchResponse>(searchCategoriesUrl.Replace("%7Bid%7D", category.Id), sessionCookie);
                await File.WriteAllTextAsync($"{category.Id}.json", searchResponse.ToJson());
                await Task.Delay(1000); // Add a small delay to avoid overwhelming the server
                                        // You can process pageResult here as needed
                                        // we need to process each page of results for the category
                foreach (var wallpaper in searchResponse!.Data)
                {
                    await GetImageDetails(sessionCookie, wallpaper.Id);
                }
                for (var i = 2; i <= searchResponse!.Meta.LastPage; i++)
                {
                    var pageUrl = searchCategoriesUrl.Replace("%7Bid%7D", category.Id) + i;
                    var pageResult = await GetFromJsonAsync<SearchResponse>(pageUrl, sessionCookie);
                    await File.WriteAllTextAsync($"{category.Id}-{i}.json", pageResult.ToJson());
                    await Task.Delay(1000); // Add a small delay to avoid overwhelming the server
                    foreach (var wallpaper in pageResult!.Data)
                    {
                        await GetImageDetails(sessionCookie, wallpaper.Id);
                    }
                    if (i == 4)
                        break;
                }
            }

            AppendStatusMessage($"Search completed in: {Stopwatch.GetElapsedTime(startTime).TotalMilliseconds} total milliseconds.");
        }
        catch (HttpRequestException e)
        {
            AppendStatusMessage($"Request error: {e.Message}");
        }
        AppendStatusMessage($"Search completed2 in: {Stopwatch.GetElapsedTime(startTime).TotalMilliseconds} total milliseconds.");
    }

    private async Task GetImageDetails(string sessionCookie, string wallpaperId)
    {
        string detailUrl = $"{BaseUrl}/w/{wallpaperId}";
        var detailResponse = await GetFromJsonAsync<DetailResponse>(detailUrl, sessionCookie);

        LogInformation($"Fetching details for wallpaper {wallpaperId}.");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        LogInformation($"Fetched details for wallpaper {wallpaperId}.");
        LogInformation($"Detail response for wallpaper {wallpaperId}: {detailResponse.Data}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        await Task.Delay(1000); // Add a small delay to avoid overwhelming the server

        foreach (var tag in detailResponse.Data.Tags)
        {
            LogInformation($"Tag for wallpaper {wallpaperId}: {tag}");
            await Task.Delay(100); // Add a small delay to avoid overwhelming the server
        }
    }

    private void LogInformation(string message)
    {
        LogMessage.Information(logger, message);
        AppendStatusMessage(message);
    }

    private void LogError(string message, Exception exception)
    {
        LogMessage.Error(logger, message, exception);
        AppendStatusMessage($"{message} {exception.Message}");
    }

    private void AppendStatusMessage(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            statusMessages.Enqueue(message);
            while (statusMessages.Count > MaximumStatusMessages)
            {
                statusMessages.Dequeue();
            }

            StatusTextBlock.Text = string.Join(Environment.NewLine, statusMessages);
            StatusScrollViewer.ScrollToEnd();
        });
    }
}

