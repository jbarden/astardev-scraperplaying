using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.UI;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAScrapeConfigurationFileService
{
    private readonly IScrapeConfigurationTransferService transferService = Substitute.For<IScrapeConfigurationTransferService>();
    private readonly IConfigurationFilePicker configurationFilePicker = Substitute.For<IConfigurationFilePicker>();
    private readonly ScrapeConfigurationFileService service;

    public GivenAScrapeConfigurationFileService()
        => service = new(transferService, configurationFilePicker);

    [Fact]
    public async Task when_a_file_is_picked_to_import_then_it_is_imported_and_some_is_returned()
    {
        configurationFilePicker.PickAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.Some("path/to/import.json"));

        var result = await service.ImportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(Unit.Instance));
        await transferService.Received(1).ImportAsync("path/to/import.json", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_file_is_picked_to_import_then_none_is_returned_and_nothing_is_imported()
    {
        configurationFilePicker.PickAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.None<string>());

        var result = await service.ImportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.None<Unit>());
        await transferService.DidNotReceive().ImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_file_is_picked_to_export_to_and_a_configuration_exists_then_it_is_exported_and_true_is_returned()
    {
        configurationFilePicker.PickSaveAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.Some("path/to/export.json"));
        transferService.ExportAsync("path/to/export.json", Arg.Any<CancellationToken>()).Returns(true);

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(true));
    }

    [Fact]
    public async Task when_a_file_is_picked_to_export_to_but_no_configuration_exists_then_false_is_returned()
    {
        configurationFilePicker.PickSaveAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.Some("path/to/export.json"));
        transferService.ExportAsync("path/to/export.json", Arg.Any<CancellationToken>()).Returns(false);

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(false));
    }

    [Fact]
    public async Task when_no_destination_file_is_picked_to_export_to_then_none_is_returned_and_nothing_is_exported()
    {
        configurationFilePicker.PickSaveAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.None<string>());

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.None<bool>());
        await transferService.DidNotReceive().ExportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
