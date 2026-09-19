using System.Diagnostics;
using AStarDev.FunctionalParadigm;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ControlDb;
using AStarDev.ScraperPlaying.UI;
using AStarDev.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

public class ScrapeService(OperationCoordinator operationCoordinator, IServiceScopeFactory scopeFactory, IRootDirectoryValidator rootDirectoryValidator) : IScrapeService
{
    /// <inheritdoc/>
    public async Task RunScraperAsync(IProgress<string> progress)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        var startTime = Stopwatch.GetTimestamp();
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var pagesProcessor = scope.ServiceProvider.GetRequiredService<IPagesProcessor>();

            progress.Report("Starting scrape operation.");
            var configuration = await ScrapeConfigurationLoader.LoadAsync(unitOfWork);

            await RunSearchesAsync(pagesProcessor, configuration, progress, cancellationToken);

            progress.Report($"Search completed in: {Stopwatch.GetElapsedTime(startTime).ToDurationString()}.");
        }
        catch (HttpRequestException e)
        {
            progress.Report($"Request error: {e.Message}");
        }
        catch (PlaywrightException e)
        {
            progress.Report($"Website scraping error: {e.Message}");
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

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ValidateRootDirectoriesAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var configuration = await ScrapeConfigurationLoader.LoadAsync(unitOfWork);

        return rootDirectoryValidator.Validate(configuration.ScrapeDirectories);
    }

    private static async Task RunSearchesAsync(IPagesProcessor pagesProcessor, ScrapeConfigurationEntity configuration, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var connection = new WallhavenConnection(configuration.UserConfiguration.ApiKey, configuration.BaseUrl, configuration.UseHeadless);

        foreach (var search in SearchPlan.Build(configuration))
        {
            if (search.CategoryName.Match(_ => false, () => true)) progress.Report("Fetching top wallpapers.");

            await pagesProcessor.FetchAndProcessPagesAsync(search, connection, progress, cancellationToken);
        }
    }
}
