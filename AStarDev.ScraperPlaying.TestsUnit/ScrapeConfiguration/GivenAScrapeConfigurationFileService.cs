using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationFileService
{
    private readonly IScrapeConfigurationImportService importService = Substitute.For<IScrapeConfigurationImportService>();
    private readonly IScrapeConfigurationExportService exportService = Substitute.For<IScrapeConfigurationExportService>();
    private readonly IConfigurationFilePicker configurationFilePicker = Substitute.For<IConfigurationFilePicker>();
    private readonly ScrapeConfigurationFileService service;

    public GivenAScrapeConfigurationFileService()
        => service = new(
            new ServiceCollection().AddScoped(_ => importService).AddScoped(_ => exportService).BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            configurationFilePicker);

    [Fact]
    public async Task when_a_file_is_picked_to_import_then_it_is_imported_and_some_is_returned()
    {
        configurationFilePicker.PickAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.Some("path/to/import.json"));

        var result = await service.ImportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(Unit.Instance));
        await importService.Received(1).ImportAsync("path/to/import.json", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_file_is_picked_to_import_then_none_is_returned_and_nothing_is_imported()
    {
        configurationFilePicker.PickAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.None<string>());

        var result = await service.ImportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.None<Unit>());
        await importService.DidNotReceive().ImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_file_is_picked_to_export_to_and_a_configuration_exists_then_it_is_exported_and_true_is_returned()
    {
        configurationFilePicker.PickSaveAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.Some("path/to/export.json"));
        exportService.ExportAsync("path/to/export.json", Arg.Any<CancellationToken>()).Returns(true);

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(true));
    }

    [Fact]
    public async Task when_a_file_is_picked_to_export_to_but_no_configuration_exists_then_false_is_returned()
    {
        configurationFilePicker.PickSaveAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.Some("path/to/export.json"));
        exportService.ExportAsync("path/to/export.json", Arg.Any<CancellationToken>()).Returns(false);

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(false));
    }

    [Fact]
    public async Task when_no_destination_file_is_picked_to_export_to_then_none_is_returned_and_nothing_is_exported()
    {
        configurationFilePicker.PickSaveAsync(Arg.Any<Avalonia.Controls.Window>()).Returns(Option.None<string>());

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.None<bool>());
        await exportService.DidNotReceive().ExportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
