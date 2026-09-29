using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAScrapeService : IDisposable
{
    private static readonly string[] expectedPersonCategories = ["Celebrities", "Models"];
    private static readonly WallhavenConnection ExpectedConnection = new("api-key", new Uri("https://example.test"));
    private readonly OperationCoordinator operationCoordinator = new();
    private readonly FakeRepository repository = new();
    private readonly FakePagesProcessor pagesProcessor = new();
    private readonly MockFileSystem fileSystem = new();
    private readonly CapturingProgress progress = new();
    private readonly ServiceProvider serviceProvider;
    private readonly ScrapeService service;

    public GivenAScrapeService()
    {
        serviceProvider = new ServiceCollection()
            .AddSingleton<IUnitOfWork>(new FakeUnitOfWork(repository))
            .AddSingleton<IPagesProcessor>(pagesProcessor)
            .BuildServiceProvider();

        service = new(operationCoordinator, serviceProvider.GetRequiredService<IServiceScopeFactory>(), fileSystem);
    }

    [Fact]
    public async Task when_a_configuration_exists_then_up_to_three_categories_then_top_wallpapers_are_processed_and_completion_is_reported()
    {
        repository.First = Found(CreateConfiguration(categoryCount: 5));

        await Run();

        progress.Messages.ShouldContain("Starting scrape operation.");
        progress.Messages.ShouldContain("Fetching top wallpapers.");
        progress.Messages.ShouldContain(message => message.StartsWith("Search completed in:"));
        pagesProcessor.Calls.Select(call => (call.LogLabel, call.CategoryName)).ShouldBe([
            ("search category category one", Option.Some("category one")),
            ("search category category two", Option.Some("category two")),
            ("search category category three", Option.Some("category three")),
            ("top wallpapers", Option.None<string>())
        ]);
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_pages_are_processed_then_every_call_uses_the_configured_connection_person_categories_and_progress()
    {
        repository.First = Found(CreateConfiguration(categoryCount: 2));

        await Run();

        pagesProcessor.Calls.ShouldAllBe(call => call.Connection == ExpectedConnection);
        pagesProcessor.Calls.ShouldAllBe(call => call.PersonCategories.SequenceEqual(expectedPersonCategories));
        pagesProcessor.Calls.ShouldAllBe(call => ReferenceEquals(call.Progress, progress));
    }

    [Fact]
    public async Task when_categories_are_processed_then_each_is_labelled_with_its_name_and_not_its_id()
    {
        repository.First = Found(CreateConfiguration(categoryCount: 3));

        await Run();

        pagesProcessor.Calls.Select(call => call.LogLabel).ShouldBe(["search category category one", "search category category two", "search category category three", "top wallpapers"]);
    }

    [Fact]
    public async Task when_no_configuration_row_exists_then_it_throws_and_still_completes_the_operation()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        await Should.ThrowAsync<InvalidOperationException>(Run);

        progress.Messages.ShouldContain("Starting scrape operation.");
        progress.Messages.ShouldNotContain(message => message.Contains("Fetching top wallpapers"));
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_looking_up_the_configuration_fails_then_the_failure_is_rethrown_and_the_operation_still_completes()
    {
        var exception = new InvalidOperationException("query failed");
        repository.First = exception;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(Run);

        thrown.ShouldBeSameAs(exception);
        progress.Messages.ShouldNotContain(message => message.Contains("not found"));
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_fetching_pages_raises_a_request_error_then_it_is_reported_not_thrown()
    {
        repository.First = Found(CreateConfiguration());
        pagesProcessor.OnFetch = () => throw new HttpRequestException("boom");

        await Run();

        progress.Messages.ShouldContain("Request error: boom");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_fetching_pages_then_cancellation_is_reported_not_thrown()
    {
        repository.First = Found(CreateConfiguration());
        pagesProcessor.OnFetch = () =>
        {
            operationCoordinator.Cancel();

            throw new OperationCanceledException();
        };

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
        pagesProcessor.Calls.ShouldBeEmpty();
        operationCoordinator.IsOperationRunning.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_root_directory_exists_on_disk_then_root_directory_exists_async_returns_true()
    {
        fileSystem.Directory.CreateDirectory("/scrapes/root");
        repository.First = Found(CreateConfiguration(rootDirectory: "/scrapes/root"));

        var exists = await service.RootDirectoryExistsAsync();

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_root_directory_does_not_exist_on_disk_then_root_directory_exists_async_returns_false()
    {
        repository.First = Found(CreateConfiguration(rootDirectory: "/scrapes/missing"));

        var exists = await service.RootDirectoryExistsAsync();

        exists.ShouldBeFalse();
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
        operationCoordinator.Dispose();
    }

    private static Exceptional<Option<ScrapeConfigurationEntity>> Found(ScrapeConfigurationEntity configuration) => (Option<ScrapeConfigurationEntity>)configuration;

    private Task Run() => service.RunScraperAsync(progress);

    private static ScrapeConfigurationEntity CreateConfiguration(int categoryCount = 1, string rootDirectory = "/scrapes/root")
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());
        var categoryNames = new[] { "category one", "category two", "category three", "category four", "category five" };
        var categories = Enumerable.Range(1, categoryCount)
            .Select(i => new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = $"cat{i}", Name = categoryNames[i - 1] })
            .ToList();

        var configuration = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            BaseUrl = new Uri("https://example.test"),
            TopWallpapers = "top/",
            SearchStringPrefix = "search/%7Bid%7D/",
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "secret", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, categories),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, rootDirectory, "famous", "sub")
        };
        configuration.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = searchConfigurationId, Name = "Celebrities" });
        configuration.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = searchConfigurationId, Name = "Models" });

        return configuration;
    }

    private sealed record PagesCall(string LogLabel, Option<string> CategoryName, WallhavenConnection Connection, IReadOnlyList<string> PersonCategories, IProgress<string> Progress);

    private sealed class FakePagesProcessor : IPagesProcessor
    {
        public List<PagesCall> Calls { get; } = [];

        public Action OnFetch { get; set; } = () => { };

        public Task FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Func<int, Uri> pageUrlFactory, WallhavenConnection connection, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Calls.Add(new PagesCall(logLabel, categoryName, connection, personCategories, progress));
            OnFetch();

            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository : IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>
    {
        public Exceptional<Option<ScrapeConfigurationEntity>> First { get; set; } = Option<ScrapeConfigurationEntity>.None.Instance;

        public Task<Exceptional<Option<ScrapeConfigurationEntity>>> TryGetFirstAsync() => Task.FromResult(First);

        public Task<Exceptional<Option<ScrapeConfigurationEntity>>> TryFindAsync(ScrapeConfigurationId key) => Task.FromResult(First);

        public Task<Exceptional<Option<IEnumerable<ScrapeConfigurationEntity>>>> TryGetAllAsync() =>
            Task.FromResult<Exceptional<Option<IEnumerable<ScrapeConfigurationEntity>>>>(Option<IEnumerable<ScrapeConfigurationEntity>>.None.Instance);

        public Exceptional<ScrapeConfigurationEntity> Add(ScrapeConfigurationEntity aggregate) => aggregate;

        public Exceptional<Unit> Delete(ScrapeConfigurationEntity aggregate) => Unit.Instance;
    }

    private sealed class FakeUnitOfWork(FakeRepository repository) : IUnitOfWork
    {
        public IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>() where TAggregate : IAggregateRoot =>
            (IRepository<TAggregate, TKey>)(object)repository;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
