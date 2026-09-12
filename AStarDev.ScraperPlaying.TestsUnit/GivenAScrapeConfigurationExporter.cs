using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAScrapeConfigurationExporter
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository = Substitute.For<IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>>();

    public GivenAScrapeConfigurationExporter() =>
        unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().Returns(repository);

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
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)existing);
        var exporter = new ScrapeConfigurationExporter(unitOfWork);

        var result = await exporter.ExportScrapeConfigurationAsync();

        var document = result.Match(option => option, exception => throw exception).Match(value => (ScrapeConfigurationImportDocument?)value, () => null);
        document.ShouldNotBeNull();
        document.UserConfiguration.Username.ShouldBe("user");
        document.SearchConfiguration.SearchTerm.ShouldBe("cats");
        document.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("General");
        document.ScrapeDirectories.RootDirectory.ShouldBe("/tmp");
    }

    [Fact]
    public async Task when_no_configuration_exists_then_none_is_returned()
    {
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)Option<ScrapeConfigurationEntity>.None.Instance);
        var exporter = new ScrapeConfigurationExporter(unitOfWork);

        var result = await exporter.ExportScrapeConfigurationAsync();

        var hasDocument = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);
        hasDocument.ShouldBeFalse();
    }

    [Fact]
    public async Task when_looking_up_the_configuration_fails_then_the_failure_is_returned()
    {
        var exception = new InvalidOperationException("lookup failed");
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)exception);
        var exporter = new ScrapeConfigurationExporter(unitOfWork);

        var result = await exporter.ExportScrapeConfigurationAsync();

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeSameAs(exception);
    }
}
