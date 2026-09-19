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

/// <summary>Exercises the real importer against a temp SQLite file.</summary>
public sealed class GivenAScrapeConfigurationImporter : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-importer-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenAScrapeConfigurationImporter()
    {
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ControlDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task when_the_same_configuration_is_imported_again_then_it_replaces_the_current_one()
    {
        var documentId = Guid.CreateVersion7();
        (await Import(CreateDocument(documentId, "first-user"))).Match(_ => true, ex => throw ex).ShouldBeTrue();

        (await Import(CreateDocument(documentId, "second-user"))).Match(_ => true, ex => throw ex).ShouldBeTrue();

        var configurations = await LoadConfigurations();
        configurations.Count.ShouldBe(1);
        configurations[0].UserConfiguration.Username.ShouldBe("second-user");
    }

    [Fact]
    public async Task when_an_import_fails_then_the_current_configuration_is_kept()
    {
        var documentId = Guid.CreateVersion7();
        await Import(CreateDocument(documentId, "first-user"));
        var failing = CreateDocument(documentId, "second-user", duplicateCategories: true);

        var result = await Import(failing);

        result.Match(_ => false, _ => true).ShouldBeTrue();
        var configurations = await LoadConfigurations();
        configurations.Count.ShouldBe(1);
        configurations[0].UserConfiguration.Username.ShouldBe("first-user");
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }

    private async Task<Exceptional<Unit>> Import(ScrapeConfigurationImportDocument document)
    {
        using var scope = serviceProvider.CreateScope();

        return await new ScrapeConfigurationImporter(scope.ServiceProvider.GetRequiredService<IUnitOfWork>()).ImportScrapeConfigurationAsync(document);
    }

    private async Task<List<ScrapeConfigurationEntity>> LoadConfigurations()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();

        return await context.ScrapeConfigurations.Include(configuration => configuration.UserConfiguration).AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
    }

    private static ScrapeConfigurationImportDocument CreateDocument(Guid documentId, string username, bool duplicateCategories = false)
        => new()
        {
            Id = documentId,
            UserConfiguration = new() { Id = Guid.CreateVersion7(), Username = username, Password = "secret" },
            SearchConfiguration = new()
            {
                Id = Guid.CreateVersion7(),
                SearchTerm = "cats",
                SearchCategories = duplicateCategories
                    ? [new() { Id = "1", Name = "one" }, new() { Id = "1", Name = "one again" }]
                    : [new() { Id = "1", Name = "one" }]
            },
            ScrapeDirectories = new() { Id = Guid.CreateVersion7(), RootDirectory = "/tmp" }
        };
}
