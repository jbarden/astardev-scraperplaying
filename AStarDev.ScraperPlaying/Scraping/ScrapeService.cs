using System.Diagnostics;
using AStarDev.ControlDb;
using AStarDev.LoggingExtensions;
using AStarDev.ScraperPlaying.Operations;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Runs a scrape as the single active operation: owns its lifecycle, timing and error reporting, and leaves the choice of searches to <see cref="ISearchOrchestrator"/>.</summary>
public class ScrapeService(OperationCoordinator operationCoordinator, IServiceScopeFactory scopeFactory, ILogger<ScrapeService> logger) : IScrapeService
{
    /// <inheritdoc/>
    public async Task RunScraperAsync(IProgress<string> progress)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        var startTime = Stopwatch.GetTimestamp();
        try
        {
            using var scope = scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var searchOrchestrator = scope.ServiceProvider.GetRequiredService<ISearchOrchestrator>();

            progress.Report("Starting scrape operation.");
            LogMessage.Information(logger, "Scrape started");
            var configuration = await unitOfWork.LoadScrapeConfigurationAsync();

            await searchOrchestrator.RunSearchesAsync(configuration, progress, cancellationToken);

            var duration = Stopwatch.GetElapsedTime(startTime).ToDurationString();
            progress.Report($"Search completed in: {duration}.");
            var completedMessage = $"Scrape completed in {duration}";
            LogMessage.Information(logger, completedMessage);
        }
        catch (HttpRequestException e)
        {
            progress.Report($"Request error: {e.Message}");
            LogMessage.Error(logger, "Scrape failed with a request error", e);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            progress.Report("Search cancelled.");
            LogMessage.Information(logger, "Scrape cancelled");
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }
}
