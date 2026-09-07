using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using AStar.Dev.Logging.Extensions;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI;
using AStarDev.ScraperPlaying.SearchAPI.DetailResponse;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using AStarDev.Utilities;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace AStarDev.ScraperPlaying.Home;

public partial class MainWindow : Window, IDisposable
{
    private const int MaximumStatusMessages = 100;
    private static readonly HttpClient client = CreateHttpClient();
    private readonly Queue<string> statusMessages = new();
    private readonly IScrapeConfigurationRepository scrapeConfigurationRepository;
    private readonly IScrapeConfigurationImportService importService;
    private readonly IConfigurationFilePicker configurationFilePicker;
    private readonly ILogger<MainWindow> logger;
    private readonly OperationCoordinator operationCoordinator = new();
    private bool isDisposing;

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

    public MainWindow(ILogger<MainWindow> logger, IScrapeConfigurationRepository scrapeConfigurationRepository, IScrapeConfigurationImportService importService, IConfigurationFilePicker configurationFilePicker)
    {
        InitializeComponent();
        this.scrapeConfigurationRepository = scrapeConfigurationRepository;
        this.importService = importService;
        this.configurationFilePicker = configurationFilePicker;
        this.logger = logger;
        operationCoordinator.StateChanged += (_, _) => UpdateOperationControls();
        Closed += (_, _) => Dispose();
        UpdateOperationControls();
    }

    public static MainWindow CreateStartupError(Exception exception)
    {
        var window = new MainWindow(NullLogger<MainWindow>.Instance, null!, null!, null!);
        window.AppendStatusMessage($"Startup failed: {exception.GetType().Name}: {exception.Message}");
        return window;
    }

    public async void ImportConfiguration(object? sender, RoutedEventArgs eventArgs)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        try
        {
            var filePath = await configurationFilePicker.PickAsync(this);
            if (filePath is null) return;

            cancellationToken.ThrowIfCancellationRequested();
            await importService.ImportAsync(filePath, cancellationToken);
            StatusTextBlock.Text = "Scrape configuration imported.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AppendStatusMessage("Scrape configuration import cancelled.");
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException)
        {
            LogError("Unable to import scrape configuration.", exception);
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    public async void RunScraper(object? sender, RoutedEventArgs eventArgs)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

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
            await FetchAndProcessPagesAsync(
                "top wallpapers",
                page => topWallpapersUrl + page,
                page => page == 1 ? "topWallpapers-1.json" : $"topWallpapers-{page}.json",
                sessionCookie,
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
                    cancellationToken);
            }

            AppendStatusMessage($"Search completed in: {Stopwatch.GetElapsedTime(startTime).TotalMilliseconds} total milliseconds.");
        }
        catch (HttpRequestException e)
        {
            AppendStatusMessage($"Request error: {e.Message}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AppendStatusMessage("Search cancelled.");
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    private async Task FetchAndProcessPagesAsync(string logLabel, Func<int, string> pageUrlFactory, Func<int, string> pageFileNameFactory, string sessionCookie, CancellationToken cancellationToken)
    {
        var page = 1;
        SearchResponse pageResult;
        do
        {
            LogInformation($"Fetching {logLabel} page {page}.");
            pageResult = (await GetFromJsonAsync<SearchResponse>(pageUrlFactory(page), sessionCookie, cancellationToken))!;
            await File.WriteAllTextAsync(pageFileNameFactory(page), pageResult.ToJson(), cancellationToken);
            await Task.Delay(1000, cancellationToken);
            foreach (var wallpaper in pageResult.Data)
            {
                await GetImageDetails(sessionCookie, wallpaper.Id, cancellationToken);
            }
            page++;
        } while (page <= pageResult.Meta.LastPage && page <= 4);
    }

    public void CancelOperation(object? sender, RoutedEventArgs eventArgs)
    {
        operationCoordinator.Cancel();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (isDisposing) return;

        if (disposing)
        {
            operationCoordinator.Dispose();
        }

        isDisposing = true;
    }

    private async Task GetImageDetails(string sessionCookie, string wallpaperId, CancellationToken cancellationToken)
    {
        string detailUrl = $"{BaseUrl}/w/{wallpaperId}";
        LogInformation($"Fetching details for wallpaper {wallpaperId}.");
        var detailResponse = await GetFromJsonAsync<DetailResponse>(detailUrl, sessionCookie, cancellationToken);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        LogInformation($"Detail response for wallpaper {wallpaperId}: {detailResponse.Data}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        await Task.Delay(1000, cancellationToken);

        await GetTags(wallpaperId, detailResponse, cancellationToken);

        var imageResponse = await client.GetAsync(detailResponse.Data.Path, cancellationToken);
        imageResponse.EnsureSuccessStatusCode();
        LogInformation($"Fetched image for wallpaper {wallpaperId}.");
        var imageData = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        LogInformation($"Downloaded image data for wallpaper {wallpaperId}, size: {imageData.Length} bytes.");
        SaveImageData(wallpaperId, imageData);
    }

    private void SaveImageData(string wallpaperId, byte[] imageData)
    {
        LogInformation($"Saving image data for wallpaper {wallpaperId}, size: {imageData.Length} bytes.");
        File.WriteAllBytes($"{wallpaperId}.jpg", imageData);
    }

    private async Task GetTags(string wallpaperId, DetailResponse detailResponse, CancellationToken cancellationToken)
    {
        foreach (var tag in detailResponse.Data.Tags)
        {
            LogInformation($"Tag for wallpaper {wallpaperId}: {tag}");
            await Task.Delay(100, cancellationToken);
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

    private void UpdateOperationControls()
    {
        var isOperationRunning = operationCoordinator.IsOperationRunning;
        ImportConfigurationButton.IsEnabled = !isOperationRunning;
        RunScraperButton.IsEnabled = !isOperationRunning;
        CancelButton.IsEnabled = isOperationRunning;
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

