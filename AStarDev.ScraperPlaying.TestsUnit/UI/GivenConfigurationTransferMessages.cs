using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.UI;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenConfigurationTransferMessages
{
    [Fact]
    public void when_an_import_completed_then_the_imported_message_is_returned()
        => ConfigurationTransferMessages.ForImport(Option.Some(Unit.Instance)).ShouldBe("Scrape configuration imported.");

    [Fact]
    public void when_an_import_was_not_completed_then_the_could_not_be_completed_message_is_returned()
        => ConfigurationTransferMessages.ForImport(Option.None<Unit>()).ShouldBe("Scrape configuration import could not be completed.");

    [Fact]
    public void when_an_export_wrote_a_configuration_then_the_exported_message_is_returned()
        => ConfigurationTransferMessages.ForExport(Option.Some(true), ApiKeyExport.Exclude).ShouldBe("Scrape configuration exported without its API keys.");

    [Fact]
    public void when_an_export_wrote_a_configuration_with_api_keys_then_the_message_says_so()
        => ConfigurationTransferMessages.ForExport(Option.Some(true), ApiKeyExport.Include).ShouldBe("Scrape configuration exported including its API keys.");

    [Fact]
    public void when_an_export_found_nothing_to_export_then_the_none_found_message_is_returned()
        => ConfigurationTransferMessages.ForExport(Option.Some(false), ApiKeyExport.Exclude).ShouldBe("No scrape configuration was found to export.");

    [Fact]
    public void when_an_export_was_not_completed_then_the_could_not_be_completed_message_is_returned()
        => ConfigurationTransferMessages.ForExport(Option.None<bool>(), ApiKeyExport.Exclude).ShouldBe("Scrape configuration export could not be completed.");
}
