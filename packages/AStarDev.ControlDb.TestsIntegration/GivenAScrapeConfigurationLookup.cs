using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAScrapeConfigurationLookup : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-configuration-lookup-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAScrapeConfigurationLookup()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        _ = context.ScrapeConfigurations.ExecuteDelete(); // EnsureCreated seeds a default configuration; these tests control the rows themselves.
    }

    [Fact]
    public async Task when_configurations_exist_then_a_header_is_listed_for_each_with_its_site_and_search_term()
    {
        var entity = ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity();
        _ = await context.ScrapeConfigurations.AddAsync(entity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var lookup = new ScrapeConfigurationLookup(context);

        var result = await lookup.ListHeadersAsync(TestContext.Current.CancellationToken);

        var headers = result.Match(list => list, exception => throw exception);
        headers.ShouldBe([new ScrapeConfigurationHeader(entity.Id, new Uri("https://example.com/scrape"), "search-config")]);
    }

    [Fact]
    public async Task when_no_configuration_exists_then_no_headers_are_listed()
    {
        var lookup = new ScrapeConfigurationLookup(context);

        var result = await lookup.ListHeadersAsync(TestContext.Current.CancellationToken);

        result.Match(list => list.Count, exception => throw exception).ShouldBe(0);
    }

    [Fact]
    public async Task when_a_configuration_exists_then_its_root_directory_is_returned()
    {
        _ = await context.ScrapeConfigurations.AddAsync(ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var lookup = new ScrapeConfigurationLookup(context);

        var result = await lookup.TryGetRootDirectoryAsync(TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).ShouldBe(Option.Some("root-save-directory"));
    }

    [Fact]
    public async Task when_no_configuration_exists_then_no_root_directory_is_returned()
    {
        var lookup = new ScrapeConfigurationLookup(context);

        var result = await lookup.TryGetRootDirectoryAsync(TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).ShouldBe(Option.None<string>());
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        context.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }
}
