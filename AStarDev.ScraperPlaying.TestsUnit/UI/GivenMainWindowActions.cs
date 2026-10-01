using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Downloads;
using AStarDev.ScraperPlaying.Operations;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Tags;
using AStarDev.ScraperPlaying.UI;
using Avalonia.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenMainWindowActions : IDisposable
{
    private readonly OperationCoordinator coordinator = new();
    private readonly StatusReporter status = new(NullLogger<StatusReporter>.Instance);
    private readonly FakeDialogHost dialogs = new();
    private readonly FakeImportService importService = new();
    private readonly FakeExportService exportService = new();
    private readonly FakeConfigurationCatalogue configurationCatalogue = new();
    private readonly FakeTagCatalogue tagCatalogue = new();
    private readonly FakeDownloadsClearer downloadsClearer = new();
    private readonly FakeScrapeService scrapeService = new();
    private readonly MainWindowActions actions;

    public GivenMainWindowActions()
    {
        var operations = new UserOperationRunner(coordinator, status);
        actions = new(
            operations,
            status,
            importService,
            exportService,
            new ConfigurationBrowser(configurationCatalogue, new ConfigurationEditorWindowFactory(new ConfigurationEditSaver(new FakeUpdater()), new MockFileSystem()), status, NullLogger<ConfigurationBrowser>.Instance),
            new TagsBrowser(tagCatalogue, status),
            new ClearDownloadsRunner(downloadsClearer, operations, status),
            new ScrapeRunner(scrapeService, status));
    }

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public async Task when_a_configuration_is_imported_then_the_user_is_told()
    {
        dialogs.PickedImportPath = Option.Some("path/to/import.json");

        await actions.ImportConfigurationAsync(dialogs);

        status.Text.ShouldBe("Scrape configuration imported.");
        importService.ImportedPaths.ShouldBe(["path/to/import.json"]);
    }

    [Fact]
    public async Task when_no_file_is_picked_to_import_then_the_user_is_told_it_was_cancelled()
    {
        dialogs.PickedImportPath = Option.None<string>();

        await actions.ImportConfigurationAsync(dialogs);

        status.Text.ShouldBe("Scrape configuration import cancelled.");
        importService.ImportedPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_import_fails_then_the_failure_is_reported_and_not_thrown()
    {
        dialogs.PickedImportPath = Option.Some("path/to/import.json");
        importService.Failure = new InvalidOperationException("import failed");

        await Should.NotThrowAsync(() => actions.ImportConfigurationAsync(dialogs));

        status.Text.ShouldBe("Unable to import scrape configuration. import failed");
    }

    [Fact]
    public async Task when_the_import_is_cancelled_then_the_user_is_told()
    {
        dialogs.PickedImportPath = Option.Some("path/to/import.json");
        importService.Failure = new OperationCanceledException(new CancellationToken(true));
        dialogs.OnPick = () => coordinator.Cancel();

        await actions.ImportConfigurationAsync(dialogs);

        status.Text.ShouldBe("Scrape configuration import cancelled.");
    }

    [Fact]
    public async Task when_another_operation_is_running_then_nothing_is_imported()
    {
        _ = coordinator.TryStart(out _);

        await actions.ImportConfigurationAsync(dialogs);

        (dialogs.PickCount, status.Text).ShouldBe((0, string.Empty));
    }

    [Theory]
    [InlineData(ApiKeyExport.Exclude, "Scrape configuration exported without its API keys.")]
    [InlineData(ApiKeyExport.Include, "Scrape configuration exported including its API keys.")]
    public async Task when_a_configuration_is_exported_then_the_user_is_told_whether_the_api_keys_were_included(ApiKeyExport apiKeys, string expected)
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exportService.ConfigurationExists = true;

        await actions.ExportConfigurationAsync(dialogs, apiKeys);

        status.Text.ShouldBe(expected);
        exportService.ExportedPaths.ShouldBe(["path/to/export.json"]);
        exportService.ExportedModes.ShouldBe([apiKeys]);
    }

    [Fact]
    public async Task when_there_is_no_configuration_to_export_then_the_user_is_told()
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exportService.ConfigurationExists = false;

        await actions.ExportConfigurationAsync(dialogs, ApiKeyExport.Exclude);

        status.Text.ShouldBe("No scrape configuration was found to export.");
    }

    [Fact]
    public async Task when_no_file_is_picked_to_export_then_the_user_is_told_it_was_cancelled()
    {
        dialogs.PickedExportPath = Option.None<string>();

        await actions.ExportConfigurationAsync(dialogs, ApiKeyExport.Exclude);

        status.Text.ShouldBe("Scrape configuration export cancelled.");
        exportService.ExportedPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_export_fails_then_the_failure_is_reported_and_not_thrown()
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exportService.Failure = new InvalidOperationException("export failed");

        await Should.NotThrowAsync(() => actions.ExportConfigurationAsync(dialogs, ApiKeyExport.Exclude));

        status.Text.ShouldBe("Unable to export scrape configuration. export failed");
    }

    [Fact]
    public async Task when_the_export_is_cancelled_then_the_user_is_told()
    {
        dialogs.PickedExportPath = Option.Some("path/to/export.json");
        exportService.Failure = new OperationCanceledException(new CancellationToken(true));
        dialogs.OnPick = () => coordinator.Cancel();

        await actions.ExportConfigurationAsync(dialogs, ApiKeyExport.Exclude);

        status.Text.ShouldBe("Scrape configuration export cancelled.");
    }

    [Fact]
    public async Task when_there_are_no_configurations_to_edit_then_no_editor_is_shown_and_the_user_is_told()
    {
        await actions.EditConfigurationAsync(dialogs);

        (dialogs.ShownCount, status.Text).ShouldBe((0, "There are no scrape configurations to edit."));
    }

    [Fact]
    public async Task when_the_configuration_editor_cannot_be_prepared_then_the_failure_is_reported_and_not_thrown()
    {
        configurationCatalogue.Failure = new InvalidOperationException("catalogue broke");

        await Should.NotThrowAsync(() => actions.EditConfigurationAsync(dialogs));

        (dialogs.ShownCount, status.Text).ShouldBe((0, "Unable to edit scrape configuration. catalogue broke"));
    }

    [Fact]
    public async Task when_there_are_no_tags_to_edit_then_no_editor_is_shown_and_the_user_is_told()
    {
        await actions.EditTagsAsync(dialogs);

        (dialogs.ShownCount, status.Text).ShouldBe((0, "There are no tags to edit."));
    }

    [Fact]
    public async Task when_the_tags_editor_cannot_be_prepared_then_the_failure_is_reported_and_not_thrown()
    {
        tagCatalogue.Failure = new InvalidOperationException("tags broke");

        await Should.NotThrowAsync(() => actions.EditTagsAsync(dialogs));

        (dialogs.ShownCount, status.Text).ShouldBe((0, "Unable to edit tags. tags broke"));
    }

    [Fact]
    public async Task when_the_person_confirms_clearing_then_the_downloads_are_cleared_and_the_user_is_told()
    {
        dialogs.Confirmed = true;
        downloadsClearer.Result = Exceptional.Success(new ClearedDownloads(3));

        await actions.ClearDownloadsAsync(dialogs);

        (downloadsClearer.ClearCount, status.Text).ShouldBe((1, "Downloads cleared: 3 file records removed and the save directories emptied."));
    }

    [Fact]
    public async Task when_the_person_declines_clearing_then_nothing_is_cleared()
    {
        dialogs.Confirmed = false;

        await actions.ClearDownloadsAsync(dialogs);

        (downloadsClearer.ClearCount, status.Text).ShouldBe((0, string.Empty));
    }

    [Fact]
    public async Task when_the_confirmation_fails_then_the_failure_is_reported_and_not_thrown()
    {
        dialogs.ConfirmFailure = new InvalidOperationException("dialog broke");

        await Should.NotThrowAsync(() => actions.ClearDownloadsAsync(dialogs));

        (downloadsClearer.ClearCount, status.Text).ShouldBe((0, "Unable to clear downloads. dialog broke"));
    }

    [Fact]
    public async Task when_the_scraper_is_run_then_its_progress_reaches_the_status()
    {
        scrapeService.OnRun = progress => progress.Report("Fetching categories.");

        await actions.RunScraperAsync();

        status.Text.ShouldBe("Fetching categories.");
    }

    [Fact]
    public void when_an_operation_is_running_and_it_is_cancelled_then_its_token_is_cancelled()
    {
        _ = coordinator.TryStart(out var cancellationToken);

        actions.CancelOperation();

        cancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public void when_no_operation_is_running_and_cancel_is_requested_then_nothing_happens()
        => Should.NotThrow(actions.CancelOperation);

    private sealed class FakeDialogHost : IDialogHost
    {
        public Option<string> PickedImportPath { get; set; } = Option.None<string>();

        public Option<string> PickedExportPath { get; set; } = Option.None<string>();

        public Exception? ConfirmFailure { get; set; }

        public bool Confirmed { get; set; }

        public Action OnPick { get; set; } = () => { };

        public int PickCount { get; private set; }

        public int ShownCount { get; private set; }

        public Task<Option<string>> PickImportFileAsync()
        {
            PickCount++;
            OnPick();

            return Task.FromResult(PickedImportPath);
        }

        public Task<Option<string>> PickExportFileAsync()
        {
            PickCount++;
            OnPick();

            return Task.FromResult(PickedExportPath);
        }

        public Task ShowAsync(Window dialog)
        {
            ShownCount++;

            return Task.CompletedTask;
        }

        public Task<bool> ConfirmAsync(string title, string message, string confirmLabel)
            => ConfirmFailure is null ? Task.FromResult(Confirmed) : Task.FromException<bool>(ConfirmFailure);
    }

    private sealed class FakeImportService : IScrapeConfigurationImportService
    {
        public Exception? Failure { get; set; }

        public List<string> ImportedPaths { get; } = [];

        public Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (Failure is not null) return Task.FromException(Failure);

            ImportedPaths.Add(filePath);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeExportService : IScrapeConfigurationExportService
    {
        public Exception? Failure { get; set; }

        public bool ConfigurationExists { get; set; }

        public List<string> ExportedPaths { get; } = [];

        public List<ApiKeyExport> ExportedModes { get; } = [];

        public Task<bool> ExportAsync(string filePath, ApiKeyExport apiKeys, CancellationToken cancellationToken = default)
        {
            if (Failure is not null) return Task.FromException<bool>(Failure);

            ExportedPaths.Add(filePath);
            ExportedModes.Add(apiKeys);

            return Task.FromResult(ConfigurationExists);
        }
    }

    private sealed class FakeUpdater : IScrapeConfigurationUpdater
    {
        public Task<Exceptional<Option<Unit>>> SaveAsync(ScrapeConfigurationId id, IReadOnlyList<IScrapeConfigurationSectionEdit> edits, CancellationToken cancellationToken)
            => Task.FromResult(Exceptional.Success(Option.Some(Unit.Instance)));
    }

    private sealed class FakeConfigurationCatalogue : IScrapeConfigurationCatalogue
    {
        public Exception? Failure { get; set; }

        public Task<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>> ListAsync(CancellationToken cancellationToken = default)
            => Failure is null
                ? Task.FromResult(Exceptional.Success<IReadOnlyList<ScrapeConfigurationSummary>>([]))
                : Task.FromException<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>>(Failure);

        public Task<Exceptional<Option<ScrapeConfigurationEntity>>> FindAsync(ScrapeConfigurationId id, CancellationToken cancellationToken = default)
            => Task.FromResult(Exceptional.Success(Option.None<ScrapeConfigurationEntity>()));
    }

    private sealed class FakeTagCatalogue : ITagCatalogue
    {
        public Exception? Failure { get; set; }

        public Task<Exceptional<IReadOnlyList<TagSummary>>> ListAsync(CancellationToken cancellationToken = default)
            => Failure is null
                ? Task.FromResult(Exceptional.Success<IReadOnlyList<TagSummary>>([]))
                : Task.FromException<Exceptional<IReadOnlyList<TagSummary>>>(Failure);

        public Task<Exceptional<Unit>> SaveFlagsAsync(IReadOnlyDictionary<int, TagFlags> flagsByWallhavenId, CancellationToken cancellationToken = default)
            => Task.FromResult(Exceptional.Success(Unit.Instance));
    }

    private sealed class FakeDownloadsClearer : IDownloadsClearer
    {
        public Exceptional<ClearedDownloads> Result { get; set; } = Exceptional.Success(new ClearedDownloads(0));

        public int ClearCount { get; private set; }

        public Task<Exceptional<ClearedDownloads>> ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCount++;

            return Task.FromResult(Result);
        }
    }

    private sealed class FakeScrapeService : IScrapeService
    {
        public Action<IProgress<string>> OnRun { get; set; } = _ => { };

        public Task RunScraperAsync(IProgress<string> progress)
        {
            OnRun(progress);

            return Task.CompletedTask;
        }
    }
}
