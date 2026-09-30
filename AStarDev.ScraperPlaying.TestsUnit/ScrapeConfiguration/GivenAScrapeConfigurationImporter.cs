using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.Extensions.DependencyInjection;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationImporter : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private int unitOfWorkResolutions;
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;
    private readonly ScrapeConfigurationImporter importer;

    public GivenAScrapeConfigurationImporter()
    {
        repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        serviceProvider = new ServiceCollection().AddScoped<IUnitOfWork>(_ =>
        {
            unitOfWorkResolutions++;

            return unitOfWork;
        }).BuildServiceProvider();
        importer = new ScrapeConfigurationImporter(serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    public void Dispose() => serviceProvider.Dispose();

    [Fact]
    public async Task when_several_imports_run_then_each_resolves_its_own_unit_of_work()
    {
        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("first"));
        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("second"));

        unitOfWorkResolutions.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_document_is_imported_then_the_delete_and_add_happen_inside_a_single_transaction()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)new ScrapeConfigurationEntity(new ScrapeConfigurationId(Guid.CreateVersion7()));

        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("user"));

        unitOfWork.TransactionCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_document_cannot_be_mapped_then_the_existing_configuration_is_not_deleted_and_nothing_is_saved()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)new ScrapeConfigurationEntity(new ScrapeConfigurationId(Guid.CreateVersion7()));
        var unmappable = new ScrapeConfigurationImportDocument { SearchConfiguration = new SearchConfigurationImportDocument { SearchCategories = [null!] } };

        var result = await importer.ImportScrapeConfigurationAsync(unmappable);

        (result.Match(_ => true, _ => false), repository.Deleted.Count, unitOfWork.SaveCount).ShouldBe((false, 0, 0));
    }

    [Fact]
    public async Task when_no_configuration_exists_then_the_document_is_added_without_a_prior_delete()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        var result = await importer.ImportScrapeConfigurationAsync(CreateDocument("user"));

        result.Match(_ => true, _ => false).ShouldBeTrue();
        repository.Deleted.ShouldBeEmpty();
        repository.Added.Single().UserConfiguration.Username.ShouldBe("user");
        unitOfWork.SaveCount.ShouldBe(1);
        unitOfWork.Operations.ShouldBe(["add", "save"]);
    }

    [Fact]
    public async Task when_a_configuration_already_exists_then_it_is_deleted_before_the_new_one_is_added()
    {
        var existing = new ScrapeConfigurationEntity(new ScrapeConfigurationId(Guid.CreateVersion7()));
        repository.First = (Option<ScrapeConfigurationEntity>)existing;

        var result = await importer.ImportScrapeConfigurationAsync(CreateDocument("new-user"));

        result.Match(_ => true, _ => false).ShouldBeTrue();
        repository.Deleted.ShouldBe([existing]);
        repository.Added.Single().UserConfiguration.Username.ShouldBe("new-user");
        unitOfWork.SaveCount.ShouldBe(2);
        unitOfWork.Operations.ShouldBe(["delete", "save", "add", "save"]);
    }

    [Fact]
    public async Task when_looking_up_the_existing_configuration_fails_then_the_failure_is_returned_and_nothing_is_written()
    {
        var exception = new InvalidOperationException("lookup failed");
        repository.First = exception;

        var result = await importer.ImportScrapeConfigurationAsync(new ScrapeConfigurationImportDocument());

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        unitOfWork.Operations.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_adding_the_new_configuration_fails_then_the_failure_is_returned_and_nothing_is_saved()
    {
        var exception = new InvalidOperationException("add failed");
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;
        repository.AddFailure = Option.Some<Exception>(exception);

        var result = await importer.ImportScrapeConfigurationAsync(new ScrapeConfigurationImportDocument());

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        unitOfWork.SaveCount.ShouldBe(0);
    }

    private static ScrapeConfigurationImportDocument CreateDocument(string username) => new()
    {
        UserConfiguration = new() { Username = username },
        SearchConfiguration = new() { SearchTerm = "cats", SearchCategories = [] },
        ScrapeDirectories = new() { RootDirectory = "/tmp" }
    };
}
