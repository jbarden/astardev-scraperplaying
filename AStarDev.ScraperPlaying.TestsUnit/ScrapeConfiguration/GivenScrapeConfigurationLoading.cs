using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenScrapeConfigurationLoading
{
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;

    public GivenScrapeConfigurationLoading() => repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();

    [Fact]
    public async Task when_a_configuration_exists_then_it_is_returned()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration();
        repository.First = (Option<ScrapeConfigurationEntity>)configuration;

        var loaded = await unitOfWork.LoadScrapeConfigurationAsync();

        loaded.ShouldBeSameAs(configuration);
    }

    [Fact]
    public async Task when_no_configuration_exists_then_it_throws_an_invalid_operation_exception()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => unitOfWork.LoadScrapeConfigurationAsync());

        thrown.Message.ShouldBe("Scrape configuration not found");
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_failure_is_rethrown()
    {
        var failure = new InvalidOperationException("query failed");
        repository.First = failure;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => unitOfWork.LoadScrapeConfigurationAsync());

        thrown.ShouldBeSameAs(failure);
    }
}
