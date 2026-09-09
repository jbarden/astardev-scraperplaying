using AStarDev.ScraperPlaying.SearchAPI;
using System.Text.Json;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAScrapeConfigurationImport
{
    [Fact]
    public async Task when_a_file_is_imported_then_the_repository_receives_the_complete_document()
    {
        var repository = Substitute.For<IScrapeConfigurationImporter>();
        var reader = Substitute.For<IScrapeConfigurationFileReader>();
        var document = new ScrapeConfigurationImportDocument
        {
            SearchConfiguration = new() { ApiKey = "api-key", SearchCategories = [new() { Id = "general" }] }
        };
        reader.ReadAsync("configuration.json", Arg.Any<CancellationToken>()).Returns(document);
        var service = new ScrapeConfigurationImportService(repository, reader);

        await service.ImportAsync("configuration.json", TestContext.Current.CancellationToken);

        await repository.Received(1).ImportScrapeConfigurationAsync(document);
    }

    [Fact]
    public async Task when_import_is_cancelled_then_file_processing_does_not_start()
    {
        var repository = Substitute.For<IScrapeConfigurationImporter>();
        var reader = Substitute.For<IScrapeConfigurationFileReader>();
        var service = new ScrapeConfigurationImportService(repository, reader);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.ImportAsync("configuration.json", cancellationTokenSource.Token));

        await reader.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_import_is_cancelled_while_reading_then_the_repository_is_not_updated()
    {
        var repository = Substitute.For<IScrapeConfigurationImporter>();
        var reader = Substitute.For<IScrapeConfigurationFileReader>();
        var document = new ScrapeConfigurationImportDocument();
        using var cancellationTokenSource = new CancellationTokenSource();
        reader.ReadAsync("configuration.json", cancellationTokenSource.Token).Returns(_ =>
        {
            cancellationTokenSource.Cancel();
            return document;
        });
        var service = new ScrapeConfigurationImportService(repository, reader);

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.ImportAsync("configuration.json", cancellationTokenSource.Token));

        await repository.DidNotReceive().ImportScrapeConfigurationAsync(Arg.Any<ScrapeConfigurationImportDocument>());
    }

    [Fact]
    public async Task when_json_is_read_then_all_nested_configuration_values_are_preserved()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, """
                {
                  "id": "11111111-1111-1111-1111-111111111111",
                  "userConfiguration": { "username": "user", "password": "secret" },
                  "searchConfiguration": {
                    "apiKey": "key", "baseUrl": "https://example.test", "loginUrl": "https://example.test/login",
                    "searchCategories": [{ "id": "1", "name": "general", "isFamous": true }]
                  },
                  "scrapeDirectories": { "rootDirectory": "/tmp/scrapes" }
                }
                """, TestContext.Current.CancellationToken);

            var document = await new ScrapeConfigurationFileReader().ReadAsync(path, TestContext.Current.CancellationToken);

            document.UserConfiguration.Username.ShouldBe("user");
            document.SearchConfiguration.ApiKey.ShouldBe("key");
            document.SearchConfiguration.SearchCategories.Single().IsFamous.ShouldBeTrue();
            document.ScrapeDirectories.RootDirectory.ShouldBe("/tmp/scrapes");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task when_application_settings_are_read_then_scrape_configuration_is_converted()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, """
                                {
                                    "logging": { "logLevel": { "default": "Warning" } },
                                    "scrapeConfiguration": {
                                        "userConfiguration": { "loginEmailAddress": "user@example.test", "username": "user", "password": "secret" },
                                        "searchConfiguration": {
                                            "baseUrl": "https://example.test", "loginUrl": "login",
                                            "searchCategories": [{ "id": "1", "name": "General", "lastPageVisited": 4, "totalPages": 8 }],
                                            "searchString": "/search", "imagePauseInSeconds": 10
                                        },
                                        "scrapeDirectories": {
                                            "baseSaveDirectory": "Pictures", "baseDirectory": "Pictures/Wallhaven",
                                            "baseDirectoryFamous": "Pictures/Famous", "subDirectoryName": "Wallhaven"
                                        }
                                    }
                                }
                                """, TestContext.Current.CancellationToken);

            var document = await new ScrapeConfigurationFileReader().ReadAsync(path, TestContext.Current.CancellationToken);

            document.UserConfiguration.EmailAddress.ShouldBe("user@example.test");
            document.SearchConfiguration.SearchCategories.Single().LastPageVisited.ShouldBe(4);
            document.ScrapeDirectories.RootDirectory.ShouldBe("Pictures/Wallhaven");
            document.Id.ShouldNotBe(Guid.Empty);
            document.SearchConfiguration.Id.ShouldNotBe(Guid.Empty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task when_json_is_malformed_then_reading_fails_with_a_json_exception()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "not json", TestContext.Current.CancellationToken);

            await Should.ThrowAsync<JsonException>(() => new ScrapeConfigurationFileReader().ReadAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void when_document_is_mapped_then_the_full_entity_graph_is_created()
    {
        var document = new ScrapeConfigurationImportDocument
        {
            Id = Guid.CreateVersion7(),
            UserConfiguration = new() { Username = "user", Password = "secret" },
            SearchConfiguration = new() { SearchTerm = "cats", SearchCategories = [new() { Id = "1", Name = "General" }] },
            ScrapeDirectories = new() { RootDirectory = "/tmp" }
        };

        var entity = document.ToEntity();

        entity.UserConfiguration.Username.ShouldBe("user");
        entity.SearchConfiguration.SearchTerm.ShouldBe("cats");
        entity.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("General");
        entity.ScrapeDirectories.RootDirectory.ShouldBe("/tmp");
    }
}