using System.Net.Http.Json;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI;
using AStarDev.ScraperPlaying.SearchAPI.DetailResponse;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.ScraperPlaying.home;

public partial class MainWindow : Window
{
    private static readonly HttpClient client = new();
    private readonly IScrapeConfigurationRepository scrapeConfigurationRepository;

    // Replace with your actual Wallhaven API key if needed SOME_FAKE_API_KEY_AS_PLACEHOLDER
    private const string ApiKey = "T5FPTPqzrpcL4jptNdB8TlpEei3smy7E";
    private const string BaseUrl = "https://wallhaven.cc/api/v1";

    public MainWindow(IScrapeConfigurationRepository scrapeConfigurationRepository)
    {
        InitializeComponent();
        this.scrapeConfigurationRepository = scrapeConfigurationRepository;
    }

    public async void DoStuff(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            var configuration = await scrapeConfigurationRepository.GetScrapeConfigurationAsync();
            var dyi = configuration.Match(
                c => c,
                _ => throw new InvalidOperationException("Scrape configuration not found")
            )!;

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
