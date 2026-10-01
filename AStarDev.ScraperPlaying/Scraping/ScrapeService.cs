using System.Diagnostics;
using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.ScraperPlaying.Operations;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.Utilities;
using AStarDev.ScraperPlaying.Scoping;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Runs a scrape as the single active operation: owns its lifecycle, timing and error reporting, and leaves the choice of searches to <see cref="ISearchOrchestrator"/>.</summary>
public sealed class ScrapeService(OperationCoordinator operationCoordinator, IScopedRunner scopedRunner, ILogger<ScrapeService> logger) : IScrapeService
{
    /// <inheritdoc/>
    public async Task RunScraperAsync(IProgress<string> progress)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        var startTime = Stopwatch.GetTimestamp();
        try
        {
            progress.Report("Starting scrape operation.");
            LogMessage.Information(logger, "Scrape started");
            _ = await scopedRunner.RunAsync<IUnitOfWork, ISearchOrchestrator, Unit>(async (unitOfWork, searchOrchestrator) =>
            {
                var configuration = await unitOfWork.LoadScrapeConfigurationAsync();
                await searchOrchestrator.RunSearchesAsync(configuration, progress, cancellationToken);

                return Unit.Instance;
            });

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
