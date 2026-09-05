using AStarDev.ScraperPlaying.SearchAPI;
using NSubstitute;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAScrapeConfigurationImport
{
    [Fact]
    public async Task when_a_file_is_imported_then_the_repository_receives_the_complete_document()
    {
        var repository = Substitute.For<IScrapeConfigurationRepository>();
        var reader = Substitute.For<IScrapeConfigurationFileReader>();
        var document = new ScrapeConfigurationImportDocument
        {
            SearchConfiguration = new() { ApiKey = "api-key", SearchCategories = [new() { Id = "general" }] }
        };
        reader.ReadAsync("configuration.json").Returns(document);
        var service = new ScrapeConfigurationImportService(repository, reader);

        await service.ImportAsync("configuration.json");

        await repository.Received(1).ImportScrapeConfigurationAsync(document);
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

            var document = await new ScrapeConfigurationFileReader().ReadAsync(path);

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
    public async Task when_json_is_malformed_then_reading_fails_with_a_json_exception()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "not json", TestContext.Current.CancellationToken);

            await Should.ThrowAsync<JsonException>(() => new ScrapeConfigurationFileReader().ReadAsync(path));
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
            Id = Guid.NewGuid(),
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