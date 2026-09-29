using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.ScrapeConfiguration;

/// <summary>Exercises <see cref="ScrapeConfigurationUpdater"/> against the real data services and a temp SQLite file.</summary>
public sealed class GivenAScrapeConfigurationUpdater : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-updater-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private readonly ScrapeConfigurationUpdater updater;
    private bool disposed;

    public GivenAScrapeConfigurationUpdater()
    {
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();
        updater = new ScrapeConfigurationUpdater(serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    [Fact]
    public async Task when_root_settings_are_saved_then_a_fresh_read_returns_them_and_the_children_are_unchanged()
    {
        var id = await SeedAsync();
        var settings = RootSettings.From(await ReadAsync(id)) with
        {
            BaseUrl = new Uri("https://other.example/api"),
            ApiKey = "new-key",
            TotalPages = 99,
            UseHeadless = true,
            SlowMotionDelay = Option.Some(125f)
        };

        var result = await updater.SaveAsync(id, [settings], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var reloaded = await ReadAsync(id);
        reloaded.BaseUrl.ShouldBe(new Uri("https://other.example/api"));
        reloaded.ApiKey.ShouldBe("new-key");
        reloaded.TotalPages.ShouldBe(99);
        reloaded.UseHeadless.ShouldBeTrue();
        reloaded.SlowMotionDelay.ShouldBe(125f);
        reloaded.SearchConfiguration.SearchTerm.ShouldBe("search-config");
        reloaded.UserConfiguration.Username.ShouldBe("username");
        reloaded.ScrapeDirectories.RootDirectory.ShouldBe("root-save-directory");
    }

    [Fact]
    public async Task when_user_settings_are_saved_then_a_fresh_read_returns_them_and_the_root_settings_are_unchanged()
    {
        var id = await SeedAsync();

        var result = await updater.SaveAsync(id, [new UserSettings("new@example.test", "new-user", "new-secret", "new-key")], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var reloaded = await ReadAsync(id);
        reloaded.UserConfiguration.EmailAddress.ShouldBe("new@example.test");
        reloaded.UserConfiguration.Username.ShouldBe("new-user");
        reloaded.UserConfiguration.Password.ShouldBe("new-secret");
        reloaded.UserConfiguration.ApiKey.ShouldBe("new-key");
        reloaded.BaseUrl.ShouldBe(new Uri("https://example.com/scrape"));
        reloaded.SearchConfiguration.SearchTerm.ShouldBe("search-config");
    }

    [Fact]
    public async Task when_directory_settings_are_saved_then_a_fresh_read_returns_them_and_the_other_sections_are_unchanged()
    {
        var id = await SeedAsync();

        var result = await updater.SaveAsync(id, [new DirectorySettings("/new/root", "/new/famous", "new-sub")], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var reloaded = await ReadAsync(id);
        reloaded.ScrapeDirectories.RootDirectory.ShouldBe("/new/root");
        reloaded.ScrapeDirectories.RootDirectoryFamous.ShouldBe("/new/famous");
        reloaded.ScrapeDirectories.SubDirectoryName.ShouldBe("new-sub");
        reloaded.UserConfiguration.Username.ShouldBe("username");
        reloaded.BaseUrl.ShouldBe(new Uri("https://example.com/scrape"));
    }

    [Fact]
    public async Task when_search_settings_are_saved_then_a_fresh_read_returns_them_and_the_other_sections_are_unchanged()
    {
        var id = await SeedAsync();

        var result = await updater.SaveAsync(id, [new SearchSettings("dogs", Option.Some(50))], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var reloaded = await ReadAsync(id);
        reloaded.SearchConfiguration.SearchTerm.ShouldBe("dogs");
        reloaded.SearchConfiguration.MaxResults.ShouldBe(50);
        reloaded.UserConfiguration.Username.ShouldBe("username");
        reloaded.ScrapeDirectories.RootDirectory.ShouldBe("root-save-directory");
    }

    [Fact]
    public async Task when_max_results_is_cleared_then_it_is_stored_as_null()
    {
        var id = await SeedAsync();

        await updater.SaveAsync(id, [new SearchSettings("search-config", Option.None<int>())], TestContext.Current.CancellationToken);

        (await ReadAsync(id)).SearchConfiguration.MaxResults.ShouldBeNull();
    }

    [Fact]
    public async Task when_the_slow_motion_delay_is_cleared_then_it_is_stored_as_null()
    {
        var id = await SeedAsync();

        await updater.SaveAsync(id, [RootSettings.From(await ReadAsync(id)) with { SlowMotionDelay = Option.None<float>() }], TestContext.Current.CancellationToken);

        (await ReadAsync(id)).SlowMotionDelay.ShouldBeNull();
    }

    [Fact]
    public async Task when_the_configuration_does_not_exist_then_none_is_returned_and_nothing_is_created()
    {
        var seededId = await SeedAsync();
        var settings = RootSettings.From(await ReadAsync(seededId));
        var countBefore = (await ListIdsAsync()).Count;

        var result = await updater.SaveAsync(new ScrapeConfigurationId(Guid.CreateVersion7()), [settings], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeFalse();
        (await ListIdsAsync()).Count.ShouldBe(countBefore);
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }

    private async Task<ScrapeConfigurationId> SeedAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var rootId = new ScrapeConfigurationId(Guid.Empty);
        var entity = new ScrapeConfigurationEntity(rootId)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.Empty), rootId, "user@example.com", "username", "password", "apiKey"),
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.Empty), rootId, "search-config", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.Empty), rootId, "root-save-directory", "root-directory-famous", "sub-directory-name"),
            BaseUrl = new Uri("https://example.com/scrape"),
            LoginUrl = new Uri("https://example.com/scrape/login"),
            SlowMotionDelay = 250f
        };
        var added = context.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().Add(entity).Match(value => value, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return added.Id;
    }

    private async Task<ScrapeConfigurationEntity> ReadAsync(ScrapeConfigurationId id)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        return (await repository.TryFindAsync(id)).Match(option => option, exception => throw exception).Match(entity => entity, () => throw new InvalidOperationException("Not found."));
    }

    private async Task<IReadOnlyList<ScrapeConfigurationId>> ListIdsAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        var all = (await repository.TryGetAllAsync()).Match(option => option, exception => throw exception);

        return all.Match(entities => (IReadOnlyList<ScrapeConfigurationId>)[.. entities.Select(entity => entity.Id)], () => []);
    }
}
