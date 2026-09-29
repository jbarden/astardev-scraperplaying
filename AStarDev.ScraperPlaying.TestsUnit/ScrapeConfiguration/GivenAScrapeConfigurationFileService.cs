using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationFileService
{
    private readonly FakeImportService importService = new();
    private readonly FakeExportService exportService = new();
    private readonly FakeFilePicker filePicker = new();
    private readonly ScrapeConfigurationFileService service;

    public GivenAScrapeConfigurationFileService()
        => service = new(importService, exportService, filePicker);

    [Fact]
    public async Task when_a_file_is_picked_to_import_then_it_is_imported_and_some_is_returned()
    {
        filePicker.PickedForImport = Option.Some("path/to/import.json");

        var result = await service.ImportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(Unit.Instance));
        importService.ImportedPaths.ShouldBe(["path/to/import.json"]);
    }

    [Fact]
    public async Task when_no_file_is_picked_to_import_then_none_is_returned_and_nothing_is_imported()
    {
        filePicker.PickedForImport = Option.None<string>();

        var result = await service.ImportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.None<Unit>());
        importService.ImportedPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_file_is_picked_to_export_to_and_a_configuration_exists_then_it_is_exported_and_true_is_returned()
    {
        filePicker.PickedForExport = Option.Some("path/to/export.json");
        exportService.ConfigurationExists = true;

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(true));
        exportService.ExportedPaths.ShouldBe(["path/to/export.json"]);
    }

    [Fact]
    public async Task when_a_file_is_picked_to_export_to_but_no_configuration_exists_then_false_is_returned()
    {
        filePicker.PickedForExport = Option.Some("path/to/export.json");
        exportService.ConfigurationExists = false;

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.Some(false));
    }

    [Fact]
    public async Task when_no_destination_file_is_picked_to_export_to_then_none_is_returned_and_nothing_is_exported()
    {
        filePicker.PickedForExport = Option.None<string>();

        var result = await service.ExportViaPickerAsync(null!, CancellationToken.None);

        result.ShouldBe(Option.None<bool>());
        exportService.ExportedPaths.ShouldBeEmpty();
    }

    private sealed class FakeImportService : IScrapeConfigurationImportService
    {
        public List<string> ImportedPaths { get; } = [];

        public Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ImportedPaths.Add(filePath);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeExportService : IScrapeConfigurationExportService
    {
        public bool ConfigurationExists { get; set; }

        public List<string> ExportedPaths { get; } = [];

        public Task<bool> ExportAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ExportedPaths.Add(filePath);

            return Task.FromResult(ConfigurationExists);
        }
    }

    private sealed class FakeFilePicker : IConfigurationFilePicker
    {
        public Option<string> PickedForImport { get; set; } = Option.None<string>();

        public Option<string> PickedForExport { get; set; } = Option.None<string>();

        public Task<Option<string>> PickAsync(Window owner) => Task.FromResult(PickedForImport);

        public Task<Option<string>> PickSaveAsync(Window owner) => Task.FromResult(PickedForExport);
    }
}
