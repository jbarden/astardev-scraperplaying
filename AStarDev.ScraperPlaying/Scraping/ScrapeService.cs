using System.Diagnostics;
using AStarDev.FunctionalParadigm;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ControlDb;
using AStarDev.ScraperPlaying.Operations;
using AStarDev.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

public class ScrapeService(OperationRunner operationRunner, IServiceScopeFactory scopeFactory, IRootDirectoryValidator rootDirectoryValidator) : IScrapeService
{
    /// <inheritdoc/>
    public Task RunScraperAsync(IProgress<string> progress)
        => operationRunner.RunAsync(
            cancellationToken => RunAsync(progress, cancellationToken),
            new OperationReporting(progress.Report, "Search cancelled.", exception => DescribeFailure(exception)));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ValidateRootDirectoriesAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var directories = await ScrapeDirectoriesLoader.LoadAsync(scope.ServiceProvider.GetRequiredService<IScrapeDirectoriesQuery>());

        return rootDirectoryValidator.Validate(directories);
    }

    private static Option<string> DescribeFailure(Exception exception)
        => exception switch
        {
            HttpRequestException => Option.Some($"Request error: {exception.Message}"),
            PlaywrightException => Option.Some($"Website scraping error: {exception.Message}"),
            _ => Option.None<string>()
        };

    private async Task RunAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();
        await using var scope = scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var pagesProcessor = scope.ServiceProvider.GetRequiredService<IPagesProcessor>();

        progress.Report("Starting scrape operation.");
        var configuration = await ScrapeConfigurationLoader.LoadAsync(unitOfWork);

        await RunSearchesAsync(pagesProcessor, configuration, progress, cancellationToken);

        progress.Report($"Search completed in: {Stopwatch.GetElapsedTime(startTime).ToDurationString()}.");
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
