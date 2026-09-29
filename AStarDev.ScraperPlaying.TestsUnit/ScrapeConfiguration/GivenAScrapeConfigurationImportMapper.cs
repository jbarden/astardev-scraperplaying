using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationImportMapper
{
    [Fact]
    public void when_the_document_has_no_max_results_then_the_entity_has_no_max_results() =>
        DocumentWith(null).ToEntity().SearchConfiguration.MaxResults.ShouldBeNull();

    [Fact]
    public void when_the_document_has_zero_max_results_then_the_entity_keeps_zero() =>
        DocumentWith(0).ToEntity().SearchConfiguration.MaxResults.ShouldBe(0);

    [Fact]
    public void when_the_document_has_max_results_then_the_entity_keeps_it() =>
        DocumentWith(25).ToEntity().SearchConfiguration.MaxResults.ShouldBe(25);

    private static ScrapeConfigurationImportDocument DocumentWith(int? maxResults) =>
        new() { SearchConfiguration = new SearchConfigurationImportDocument { MaxResults = maxResults } };
}
