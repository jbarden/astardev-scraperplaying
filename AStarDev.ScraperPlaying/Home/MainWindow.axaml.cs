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
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.Home;

public partial class MainWindow : Window
{
    private static readonly HttpClient client = CreateHttpClient();
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

        return await response.Content.ReadFromJsonAsync<T>();
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
        var window = new MainWindow(
            NullLogger<MainWindow>.Instance,
            null!,
            null!,
            null!);
        window.StatusTextBlock.Text = $"Startup failed: {exception.GetType().Name}: {exception.Message}\n\n{exception}";
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
            LogMessage.Error(logger, "Unable to import scrape configuration.", exception);
            StatusTextBlock.Text = $"Configuration import failed: {exception.Message}";
        }
    }

    public async void DoStuff(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            var configuration = (await scrapeConfigurationRepository.GetScrapeConfigurationAsync())
            .Match(
                c => c,
                _ => throw new InvalidOperationException("Scrape configuration not found")
            )!;
            System.Console.WriteLine(configuration);
            var apiKey = configuration.UserConfiguration.ApiKey;
            var sessionCookie = configuration.UserConfiguration.SessionCookie;
            var encodedApiKey = Uri.EscapeDataString(apiKey);
            var subscriptionsUrl = (configuration.SubscribedUrl.AbsoluteUri + "1")
                .Replace("%7BapiKey%7D", encodedApiKey);
            var topWallpapersUrl = (configuration.TopWallpapersUrl.AbsoluteUri + "1")
                .Replace("%7BapiKey%7D", encodedApiKey);
            var searchCategoriesUrl = (configuration.SearchCategoriesUrl.AbsoluteUri + "1")
                .Replace("%7BapiKey%7D", encodedApiKey);
            var searchCategories = configuration.SearchCategories;
#pragma warning disable CA1873 // Avoid potentially expensive logging
            LogMessage.Information(logger, "Scrape configuration: {Configuration}", configuration.ToJson());
            LogMessage.Information(logger, "apiKey: {ApiKey}", apiKey);
            LogMessage.Information(logger, "subscriptionsUrl: {SubscriptionsUrl}", subscriptionsUrl);
            LogMessage.Information(logger, "topWallpapersUrl: {TopWallpapersUrl}", topWallpapersUrl);
            LogMessage.Information(logger, "searchCategoriesUrl: {SearchCategoriesUrl}", searchCategoriesUrl);
#pragma warning restore CA1873 // Avoid potentially expensive logging

            var result = await GetFromJsonAsync<SearchResponse>(subscriptionsUrl, sessionCookie);
            var result2 = await GetFromJsonAsync<SearchResponse>(topWallpapersUrl, sessionCookie);
            var result3 = await GetFromJsonAsync<SearchResponse>(
                searchCategoriesUrl.Replace("%7Bid%7D", "1111111"), sessionCookie);

            Console.WriteLine("Searching Wallhaven for 'cyberpunk' wallpapers...");

            // 1. Search for wallpapers
            string searchUrl = $"{BaseUrl}/search?q=cyberpunk&categories=111&purity=100&apikey={apiKey}";
            var searchResponse = await GetFromJsonAsync<SearchResponse>(searchUrl, sessionCookie);

            Console.WriteLine("\nSearch Results JSON Summary:");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Console.WriteLine(string.Concat(searchResponse.Data.First().ToString().AsSpan(0, 500), "...")); // Shows first 500 characters
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            StatusTextBlock.Text = $"Search completed: {searchResponse}";

            // 2. Fetch specific wallpaper details (Example ID: 8527o1)
            string wallpaperId = "yq9zqk";
            string detailUrl = $"{BaseUrl}/w/{wallpaperId}";
            var detailResponse = await GetFromJsonAsync<DetailResponse>(detailUrl, sessionCookie);

            Console.WriteLine($"\nDetails for Wallpaper {wallpaperId}:");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Console.WriteLine(string.Concat(detailResponse.Data.ToString().AsSpan(0, 500), "..."));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            StatusTextBlock.Text += $"\nDetails fetched for wallpaper {wallpaperId}: {detailResponse}";
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
        }
    }
}
