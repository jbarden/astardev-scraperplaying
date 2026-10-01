using System.Text.Json;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;

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

    [Fact]
    public void when_a_configuration_with_hot_wallpapers_is_exported_and_imported_then_the_hot_settings_are_kept()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration();
        configuration.HotWallpapers = "hot/";
        configuration.HotWallpapersStartingPageNumber = 5;
        configuration.HotWallpapersTotalPages = 55;

        var imported = configuration.ToImportDocument(ApiKeyExport.Exclude).ToEntity();

        (imported.HotWallpapers, imported.HotWallpapersStartingPageNumber, imported.HotWallpapersTotalPages).ShouldBe(("hot/", 5, 55));
    }

    [Fact]
    public void when_an_exported_file_has_no_hot_wallpapers_settings_then_the_url_defaults_and_the_pages_are_zero()
    {
        var document = JsonSerializer.Deserialize<ScrapeConfigurationImportDocument>("{}")!;

        var imported = document.ToEntity();

        (imported.HotWallpapers, imported.HotWallpapersStartingPageNumber, imported.HotWallpapersTotalPages).ShouldBe(("HotWallpapers", 0, 0));
    }

    private static ScrapeConfigurationImportDocument DocumentWith(int? maxResults) =>
        new() { SearchConfiguration = new SearchConfigurationImportDocument { MaxResults = maxResults } };
}
