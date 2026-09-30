using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using System.Text.Json;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationExport
{
    [Fact]
    public async Task when_a_configuration_exists_then_it_is_written_to_the_destination_file()
    {
        var document = new ScrapeConfigurationImportDocument
        {
            ApiKey = "api-key",
            SearchConfiguration = new() { SearchCategories = [new() { Id = "general" }] }
        };
        var exporter = new FakeExporter { Result = (Option<ScrapeConfigurationImportDocument>)document };
        var writer = new FakeWriter();
        var service = new ScrapeConfigurationExportService(exporter, writer);

        var exported = await service.ExportAsync("configuration.json", TestContext.Current.CancellationToken);

        exported.ShouldBeTrue();
        writer.Written.ShouldBe([(document, "configuration.json")]);
    }

    [Fact]
    public async Task when_no_configuration_exists_then_nothing_is_written()
    {
        var exporter = new FakeExporter { Result = Option<ScrapeConfigurationImportDocument>.None.Instance };
        var writer = new FakeWriter();
        var service = new ScrapeConfigurationExportService(exporter, writer);

        var exported = await service.ExportAsync("configuration.json", TestContext.Current.CancellationToken);

        exported.ShouldBeFalse();
        writer.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_exception_propagates()
    {
        var exception = new InvalidOperationException("lookup failed");
        var exporter = new FakeExporter { Result = exception };
        var writer = new FakeWriter();
        var service = new ScrapeConfigurationExportService(exporter, writer);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => service.ExportAsync("configuration.json", TestContext.Current.CancellationToken));

        thrown.ShouldBeSameAs(exception);
        writer.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_export_is_cancelled_then_the_export_does_not_start()
    {
        var exporter = new FakeExporter();
        var service = new ScrapeConfigurationExportService(exporter, new FakeWriter());
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.ExportAsync("configuration.json", cancellationTokenSource.Token));

        exporter.LookupCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_document_is_written_then_the_json_file_contains_the_full_configuration()
    {
        var path = Path.GetTempFileName();
        try
        {
            var document = new ScrapeConfigurationImportDocument
            {
                UserConfiguration = new() { Username = "user" },
                SearchConfiguration = new() { SearchTerm = "cats", SearchCategories = [new() { Id = "1", Name = "General", IsFamous = true }] },
                ScrapeDirectories = new() { RootDirectory = "/tmp/scrapes" }
            };

            await new ScrapeConfigurationFileWriter().WriteAsync(document, path, TestContext.Current.CancellationToken);
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));

            json.RootElement.GetProperty("userConfiguration").GetProperty("username").GetString().ShouldBe("user");
            json.RootElement.GetProperty("searchConfiguration").GetProperty("searchTerm").GetString().ShouldBe("cats");
            json.RootElement.GetProperty("searchConfiguration").GetProperty("searchCategories")[0].GetProperty("isFamous").GetBoolean().ShouldBeTrue();
            json.RootElement.GetProperty("scrapeDirectories").GetProperty("rootDirectory").GetString().ShouldBe("/tmp/scrapes");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void when_an_entity_is_mapped_then_the_full_document_graph_is_created()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());
        var entity = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, [
                new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = "1", Name = "General" }
            ]),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, "/tmp", "Pictures/Famous", "Wallhaven")
        };
        entity.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = searchConfigurationId, Name = "Drivers" });

        var document = entity.ToImportDocument();

        document.UserConfiguration.Username.ShouldBe("user");
        document.SearchConfiguration.SearchTerm.ShouldBe("cats");
        document.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("General");
        document.SearchConfiguration.PersonCategories.ShouldBe(["Drivers"]);
        document.ScrapeDirectories.RootDirectory.ShouldBe("/tmp");
    }

    private sealed class FakeExporter : IScrapeConfigurationExporter
    {
        public Exceptional<Option<ScrapeConfigurationImportDocument>> Result { get; set; } = Option<ScrapeConfigurationImportDocument>.None.Instance;

        public int LookupCount { get; private set; }

        public Task<Exceptional<Option<ScrapeConfigurationImportDocument>>> ExportScrapeConfigurationAsync()
        {
            LookupCount++;

            return Task.FromResult(Result);
        }
    }

    private sealed class FakeWriter : IScrapeConfigurationFileWriter
    {
        public List<(ScrapeConfigurationImportDocument Document, string FilePath)> Written { get; } = [];

        public Task WriteAsync(ScrapeConfigurationImportDocument document, string filePath, CancellationToken cancellationToken = default)
        {
            Written.Add((document, filePath));

            return Task.CompletedTask;
        }
    }
}
