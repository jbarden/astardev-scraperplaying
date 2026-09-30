using System.Diagnostics;
using AStarDev.ControlDb;
using AStarDev.ScraperPlaying.Operations;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Runs a scrape as the single active operation: owns its lifecycle, timing and error reporting, and leaves the choice of searches to <see cref="ISearchOrchestrator"/>.</summary>
public class ScrapeService(OperationCoordinator operationCoordinator, IServiceScopeFactory scopeFactory) : IScrapeService
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
            var configuration = await unitOfWork.LoadScrapeConfigurationAsync();

            await searchOrchestrator.RunSearchesAsync(configuration, progress, cancellationToken);

            progress.Report($"Search completed in: {Stopwatch.GetElapsedTime(startTime).ToDurationString()}.");
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
}
