using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationCatalogue : IDisposable
{
    private readonly IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository = Substitute.For<IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>>();
    private readonly ServiceProvider serviceProvider;
    private readonly ScrapeConfigurationCatalogue catalogue;

    public GivenAScrapeConfigurationCatalogue()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().Returns(repository);
        serviceProvider = new ServiceCollection().AddScoped(_ => unitOfWork).BuildServiceProvider();
        catalogue = new ScrapeConfigurationCatalogue(serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    [Fact]
    public async Task when_configurations_exist_then_a_summary_is_listed_for_each()
    {
        var first = CreateEntity("cats");
        var second = CreateEntity("dogs");
        repository.TryGetAllAsync().Returns(Success<IEnumerable<ScrapeConfigurationEntity>>([first, second]));

        var result = await catalogue.ListAsync();

        var summaries = result.Match(list => list, exception => throw exception);
        summaries.Select(summary => summary.Id).ShouldBe([first.Id, second.Id]);
        summaries.Select(summary => summary.Label).ShouldBe(["example.com - cats", "example.com - dogs"]);
    }

    [Fact]
    public async Task when_no_configurations_exist_then_the_list_is_empty()
    {
        repository.TryGetAllAsync().Returns((Exceptional<Option<IEnumerable<ScrapeConfigurationEntity>>>)Option<IEnumerable<ScrapeConfigurationEntity>>.None.Instance);

        var result = await catalogue.ListAsync();

        result.Match(list => list, exception => throw exception).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_listing_fails_then_the_failure_is_returned()
    {
        var failure = new InvalidOperationException("list failed");
        repository.TryGetAllAsync().Returns((Exceptional<Option<IEnumerable<ScrapeConfigurationEntity>>>)failure);

        var result = await catalogue.ListAsync();

        result.Match(_ => (Exception?)null, exception => exception).ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_a_configuration_is_found_then_it_is_returned()
    {
        var entity = CreateEntity("cats");
        repository.TryFindAsync(entity.Id).Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)entity);

        var result = await catalogue.FindAsync(entity.Id);

        result.Match(option => option, exception => throw exception).Match(found => found.Id, () => default).ShouldBe(entity.Id);
    }

    [Fact]
    public async Task when_a_configuration_is_not_found_then_none_is_returned()
    {
        repository.TryFindAsync(Arg.Any<ScrapeConfigurationId>()).Returns((Exceptional<Option<ScrapeConfigurationEntity>>)Option<ScrapeConfigurationEntity>.None.Instance);

        var result = await catalogue.FindAsync(new ScrapeConfigurationId(Guid.CreateVersion7()));

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeFalse();
    }

    [Fact]
    public async Task when_finding_fails_then_the_failure_is_returned()
    {
        var failure = new InvalidOperationException("find failed");
        repository.TryFindAsync(Arg.Any<ScrapeConfigurationId>()).Returns((Exceptional<Option<ScrapeConfigurationEntity>>)failure);

        var result = await catalogue.FindAsync(new ScrapeConfigurationId(Guid.CreateVersion7()));

        result.Match(_ => (Exception?)null, exception => exception).ShouldBeSameAs(failure);
    }

    public void Dispose() => serviceProvider.Dispose();

    private static Exceptional<Option<T>> Success<T>(T value) => (Option<T>)value;

    private static ScrapeConfigurationEntity CreateEntity(string searchTerm)
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, searchTerm, 10, [])
        };
    }
}
