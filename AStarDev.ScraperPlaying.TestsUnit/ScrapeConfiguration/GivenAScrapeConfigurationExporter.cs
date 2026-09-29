using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationExporter
{
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;

    public GivenAScrapeConfigurationExporter() =>
        repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();

    [Fact]
    public async Task when_a_configuration_exists_then_it_is_returned_as_a_document()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());
        var existing = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "secret", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, [
                new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = "1", Name = "General" }
            ]),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, "Pictures", "Pictures/Famous", "Wallhaven")
        };
        repository.First = (Option<ScrapeConfigurationEntity>)existing;
        var exporter = new ScrapeConfigurationExporter(unitOfWork);

        var result = await exporter.ExportScrapeConfigurationAsync();

        var document = result.Match(option => option, exception => throw exception).Match(value => (ScrapeConfigurationImportDocument?)value, () => null);
        document.ShouldNotBeNull();
        document.UserConfiguration.Username.ShouldBe("user");
        document.SearchConfiguration.SearchTerm.ShouldBe("cats");
        document.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("General");
        document.ScrapeDirectories.RootDirectory.ShouldBe("Pictures");
    }

    [Fact]
    public async Task when_no_configuration_exists_then_none_is_returned()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;
        var exporter = new ScrapeConfigurationExporter(unitOfWork);

        var result = await exporter.ExportScrapeConfigurationAsync();

        var hasDocument = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);
        hasDocument.ShouldBeFalse();
    }

    [Fact]
    public async Task when_looking_up_the_configuration_fails_then_the_failure_is_returned()
    {
        var exception = new InvalidOperationException("lookup failed");
        repository.First = exception;
        var exporter = new ScrapeConfigurationExporter(unitOfWork);

        var result = await exporter.ExportScrapeConfigurationAsync();

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeSameAs(exception);
    }
}
