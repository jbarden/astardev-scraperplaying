using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Home;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAScrapeService : IDisposable
{
    private readonly OperationCoordinator operationCoordinator = new();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository = Substitute.For<IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>>();
    private readonly IPagesProcessor pagesProcessor = Substitute.For<IPagesProcessor>();
    private readonly MockFileSystem fileSystem = new();
    private readonly CapturingProgress progress = new();
    private readonly ScrapeService service;

    public GivenAScrapeService()
    {
        unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().Returns(repository);
        pagesProcessor.FetchAndProcessPagesAsync(Arg.Any<string>(), Arg.Any<Option<string>>(), Arg.Any<Func<int, string>>(), Arg.Any<string>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(unitOfWork);
        services.AddSingleton(pagesProcessor);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        service = new(operationCoordinator, scopeFactory, fileSystem);
    }

    [Fact]
    public async Task when_a_configuration_exists_then_top_wallpapers_and_up_to_three_categories_are_processed_and_completion_is_reported()
    {
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)CreateConfiguration(categoryCount: 5));

        await Run();

        progress.Messages.ShouldContain("Starting scrape operation.");
        progress.Messages.ShouldContain("Fetching top wallpapers.");
        progress.Messages.ShouldContain(message => message.StartsWith("Search completed in:"));
        await pagesProcessor.Received(1).FetchAndProcessPagesAsync("top wallpapers", Option.None<string>(), Arg.Any<Func<int, string>>(), "api-key", new Uri("https://example.test"), progress, Arg.Any<CancellationToken>());
        await pagesProcessor.Received(1).FetchAndProcessPagesAsync("search category cat1", Option.Some("category one"), Arg.Any<Func<int, string>>(), "api-key", new Uri("https://example.test"), progress, Arg.Any<CancellationToken>());
        await pagesProcessor.Received(1).FetchAndProcessPagesAsync("search category cat2", Option.Some("category two"), Arg.Any<Func<int, string>>(), "api-key", new Uri("https://example.test"), progress, Arg.Any<CancellationToken>());
        await pagesProcessor.Received(1).FetchAndProcessPagesAsync("search category cat3", Option.Some("category three"), Arg.Any<Func<int, string>>(), "api-key", new Uri("https://example.test"), progress, Arg.Any<CancellationToken>());
        await pagesProcessor.DidNotReceive().FetchAndProcessPagesAsync("search category cat4", Arg.Any<Option<string>>(), Arg.Any<Func<int, string>>(), Arg.Any<string>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
        await pagesProcessor.DidNotReceive().FetchAndProcessPagesAsync("search category cat5", Arg.Any<Option<string>>(), Arg.Any<Func<int, string>>(), Arg.Any<string>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_no_configuration_row_exists_then_it_throws_and_still_completes_the_operation()
    {
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)Option<ScrapeConfigurationEntity>.None.Instance);

        await Should.ThrowAsync<InvalidOperationException>(Run);

        progress.Messages.ShouldContain("Starting scrape operation.");
        progress.Messages.ShouldNotContain(message => message.Contains("Fetching top wallpapers"));
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_looking_up_the_configuration_fails_then_the_failure_is_rethrown_and_the_operation_still_completes()
    {
        var exception = new InvalidOperationException("query failed");
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)exception);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(Run);

        thrown.ShouldBeSameAs(exception);
        progress.Messages.ShouldNotContain(message => message.Contains("not found"));
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_fetching_pages_raises_a_request_error_then_it_is_reported_not_thrown()
    {
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)CreateConfiguration());
        pagesProcessor.FetchAndProcessPagesAsync(Arg.Any<string>(), Arg.Any<Option<string>>(), Arg.Any<Func<int, string>>(), Arg.Any<string>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new HttpRequestException("boom"));

        await Run();

        progress.Messages.ShouldContain("Request error: boom");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_fetching_pages_then_cancellation_is_reported_not_thrown()
    {
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)CreateConfiguration());
        pagesProcessor.FetchAndProcessPagesAsync(Arg.Any<string>(), Arg.Any<Option<string>>(), Arg.Any<Func<int, string>>(), Arg.Any<string>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                operationCoordinator.Cancel();

                throw new OperationCanceledException();
            });

        await Run();

        progress.Messages.ShouldContain("Search cancelled.");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_an_operation_is_already_running_then_a_second_call_is_a_no_op()
    {
        operationCoordinator.TryStart(out _);

        await Run();

        progress.Messages.ShouldBeEmpty();
        await pagesProcessor.DidNotReceive().FetchAndProcessPagesAsync(Arg.Any<string>(), Arg.Any<Option<string>>(), Arg.Any<Func<int, string>>(), Arg.Any<string>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
        operationCoordinator.IsOperationRunning.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_root_directory_exists_on_disk_then_root_directory_exists_async_returns_true()
    {
        fileSystem.Directory.CreateDirectory("/scrapes/root");
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)CreateConfiguration(rootDirectory: "/scrapes/root"));

        var exists = await service.RootDirectoryExistsAsync();

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_root_directory_does_not_exist_on_disk_then_root_directory_exists_async_returns_false()
    {
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)CreateConfiguration(rootDirectory: "/scrapes/missing"));

        var exists = await service.RootDirectoryExistsAsync();

        exists.ShouldBeFalse();
    }

    public void Dispose() => operationCoordinator.Dispose();

    private Task Run() => service.RunScraperAsync(progress);

    private static ScrapeConfigurationEntity CreateConfiguration(int categoryCount = 1, string rootDirectory = "/scrapes/root")
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());
        var categoryNames = new[] { "category one", "category two", "category three", "category four", "category five" };
        var categories = Enumerable.Range(1, categoryCount)
            .Select(i => new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = $"cat{i}", Name = categoryNames[i - 1] })
            .ToList();

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            BaseUrl = new Uri("https://example.test"),
            TopWallpapers = "top/",
            SearchStringPrefix = "search/%7Bid%7D/",
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "secret", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, categories),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, rootDirectory, "famous", "sub")
        };
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
