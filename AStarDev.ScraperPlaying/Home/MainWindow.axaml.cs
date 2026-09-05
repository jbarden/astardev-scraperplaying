using System.Net.Http.Json;
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

namespace AStarDev.ScraperPlaying.Home;

public partial class MainWindow : Window
{
    private static readonly HttpClient client = new();
    private readonly IScrapeConfigurationRepository scrapeConfigurationRepository;
    private readonly IScrapeConfigurationImportService importService;
    private readonly IConfigurationFilePicker configurationFilePicker;
    private readonly ILogger<MainWindow> logger;

    // Replace with your actual Wallhaven API key if needed SOME_FAKE_API_KEY_AS_PLACEHOLDER
    private const string ApiKey = "T5FPTPqzrpcL4jptNdB8TlpEei3smy7E";
    private const string BaseUrl = "https://wallhaven.cc/api/v1";

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
#pragma warning disable CA1873 // Avoid potentially expensive logging
            LogMessage.Information(logger, "Scrape configuration: {Configuration}", configuration.ToJson());
#pragma warning restore CA1873 // Avoid potentially expensive logging

            Console.WriteLine("Searching Wallhaven for 'cyberpunk' wallpapers...");

            // 1. Search for wallpapers
            string searchUrl = $"{BaseUrl}/search?q=cyberpunk&categories=111&purity=100&apikey={ApiKey}";
            var searchResponse = await client.GetFromJsonAsync<SearchResponse>(searchUrl);

            Console.WriteLine("\nSearch Results JSON Summary:");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Console.WriteLine(string.Concat(searchResponse.Data.First().ToString().AsSpan(0, 500), "...")); // Shows first 500 characters
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            StatusTextBlock.Text = $"Search completed: {searchResponse}";

            // 2. Fetch specific wallpaper details (Example ID: 8527o1)
            string wallpaperId = "yq9zqk";
            string detailUrl = $"{BaseUrl}/w/{wallpaperId}";
            var detailResponse = await client.GetFromJsonAsync<DetailResponse>(detailUrl);

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
