using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAScrapeConfigurationImporter
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository = Substitute.For<IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>>();

    public GivenAScrapeConfigurationImporter()
    {
        unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().Returns(repository);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task when_no_configuration_exists_then_the_document_is_added_without_a_prior_delete()
    {
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)Option<ScrapeConfigurationEntity>.None.Instance);
        repository.Add(Arg.Any<ScrapeConfigurationEntity>()).Returns(callInfo => (Exceptional<ScrapeConfigurationEntity>)callInfo.Arg<ScrapeConfigurationEntity>());
        var document = new ScrapeConfigurationImportDocument
        {
            UserConfiguration = new() { Username = "user", Password = "secret" },
            SearchConfiguration = new() { SearchTerm = "cats", SearchCategories = [] },
            ScrapeDirectories = new() { RootDirectory = "/tmp" }
        };
        var importer = new ScrapeConfigurationImporter(unitOfWork);

        var result = await importer.ImportScrapeConfigurationAsync(document);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        repository.DidNotReceive().Delete(Arg.Any<ScrapeConfigurationEntity>());
        repository.Received(1).Add(Arg.Is<ScrapeConfigurationEntity>(entity => entity.UserConfiguration.Username == "user"));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_configuration_already_exists_then_it_is_deleted_before_the_new_one_is_added()
    {
        var existing = new ScrapeConfigurationEntity(new ScrapeConfigurationId(Guid.CreateVersion7()));
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)existing);
        repository.Delete(existing).Returns(UnitFp.Instance);
        repository.Add(Arg.Any<ScrapeConfigurationEntity>()).Returns(callInfo => (Exceptional<ScrapeConfigurationEntity>)callInfo.Arg<ScrapeConfigurationEntity>());
        var document = new ScrapeConfigurationImportDocument
        {
            UserConfiguration = new() { Username = "new-user", Password = "secret" },
            SearchConfiguration = new() { SearchTerm = "cats", SearchCategories = [] },
            ScrapeDirectories = new() { RootDirectory = "/tmp" }
        };
        var importer = new ScrapeConfigurationImporter(unitOfWork);

        var result = await importer.ImportScrapeConfigurationAsync(document);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        repository.Received(1).Delete(existing);
        repository.Received(1).Add(Arg.Is<ScrapeConfigurationEntity>(entity => entity.UserConfiguration.Username == "new-user"));
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_looking_up_the_existing_configuration_fails_then_the_failure_is_returned_and_nothing_is_written()
    {
        var exception = new InvalidOperationException("lookup failed");
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)exception);
        var importer = new ScrapeConfigurationImporter(unitOfWork);

        var result = await importer.ImportScrapeConfigurationAsync(new ScrapeConfigurationImportDocument());

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeSameAs(exception);
        repository.DidNotReceive().Add(Arg.Any<ScrapeConfigurationEntity>());
        repository.DidNotReceive().Delete(Arg.Any<ScrapeConfigurationEntity>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_adding_the_new_configuration_fails_then_the_failure_is_returned()
    {
        repository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)Option<ScrapeConfigurationEntity>.None.Instance);
        var exception = new InvalidOperationException("add failed");
        repository.Add(Arg.Any<ScrapeConfigurationEntity>()).Returns((Exceptional<ScrapeConfigurationEntity>)exception);
        var importer = new ScrapeConfigurationImporter(unitOfWork);

        var result = await importer.ImportScrapeConfigurationAsync(new ScrapeConfigurationImportDocument());

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeSameAs(exception);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
