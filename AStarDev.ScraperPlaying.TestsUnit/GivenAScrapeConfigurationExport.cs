using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI;
using System.Text.Json;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAScrapeConfigurationExport
{
    [Fact]
    public async Task when_a_configuration_exists_then_it_is_written_to_the_destination_file()
    {
        var exporter = Substitute.For<IScrapeConfigurationExporter>();
        var writer = Substitute.For<IScrapeConfigurationFileWriter>();
        var document = new ScrapeConfigurationImportDocument
        {
            SearchConfiguration = new() { ApiKey = "api-key", SearchCategories = [new() { Id = "general" }] }
        };
        exporter.ExportScrapeConfigurationAsync().Returns((Exceptional<Option<ScrapeConfigurationImportDocument>>)(Option<ScrapeConfigurationImportDocument>)document);
        var service = new ScrapeConfigurationExportService(exporter, writer);

        var exported = await service.ExportAsync("configuration.json", TestContext.Current.CancellationToken);

        exported.ShouldBeTrue();
        await writer.Received(1).WriteAsync(document, "configuration.json", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_configuration_exists_then_nothing_is_written()
    {
        var exporter = Substitute.For<IScrapeConfigurationExporter>();
        var writer = Substitute.For<IScrapeConfigurationFileWriter>();
        exporter.ExportScrapeConfigurationAsync().Returns((Exceptional<Option<ScrapeConfigurationImportDocument>>)Option<ScrapeConfigurationImportDocument>.None.Instance);
        var service = new ScrapeConfigurationExportService(exporter, writer);

        var exported = await service.ExportAsync("configuration.json", TestContext.Current.CancellationToken);

        exported.ShouldBeFalse();
        await writer.DidNotReceive().WriteAsync(Arg.Any<ScrapeConfigurationImportDocument>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_exception_propagates()
    {
        var exporter = Substitute.For<IScrapeConfigurationExporter>();
        var writer = Substitute.For<IScrapeConfigurationFileWriter>();
        var exception = new InvalidOperationException("lookup failed");
        exporter.ExportScrapeConfigurationAsync().Returns((Exceptional<Option<ScrapeConfigurationImportDocument>>)exception);
        var service = new ScrapeConfigurationExportService(exporter, writer);

        await Should.ThrowAsync<InvalidOperationException>(
            () => service.ExportAsync("configuration.json", TestContext.Current.CancellationToken));

        await writer.DidNotReceive().WriteAsync(Arg.Any<ScrapeConfigurationImportDocument>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_export_is_cancelled_then_the_export_does_not_start()
    {
        var exporter = Substitute.For<IScrapeConfigurationExporter>();
        var writer = Substitute.For<IScrapeConfigurationFileWriter>();
        var service = new ScrapeConfigurationExportService(exporter, writer);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.ExportAsync("configuration.json", cancellationTokenSource.Token));

        await exporter.DidNotReceive().ExportScrapeConfigurationAsync();
    }

    [Fact]
    public async Task when_a_document_is_written_then_the_json_file_contains_the_full_configuration()
    {
        var path = Path.GetTempFileName();
        try
        {
            var document = new ScrapeConfigurationImportDocument
            {
                UserConfiguration = new() { Username = "user", Password = "secret" },
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
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "secret", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, [
                new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = "1", Name = "General" }
            ]),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, "/tmp", "Pictures/Famous", "Wallhaven")
        };

        var document = entity.ToImportDocument();

        document.UserConfiguration.Username.ShouldBe("user");
        document.SearchConfiguration.SearchTerm.ShouldBe("cats");
        document.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("General");
        document.ScrapeDirectories.RootDirectory.ShouldBe("/tmp");
    }
}
