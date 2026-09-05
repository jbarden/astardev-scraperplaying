using System.Text.Json;

namespace AStarDev.ScraperPlaying.SearchAPI;

public interface IScrapeConfigurationImportService
{
    Task ImportAsync(string filePath);
}

public sealed class ScrapeConfigurationImportService(
    IScrapeConfigurationRepository repository,
    IScrapeConfigurationFileReader fileReader) : IScrapeConfigurationImportService
{
    public async Task ImportAsync(string filePath)
    {
        var document = await fileReader.ReadAsync(filePath);
        await repository.ImportScrapeConfigurationAsync(document);
    }
}

public interface IScrapeConfigurationFileReader
{
    Task<ScrapeConfigurationImportDocument> ReadAsync(string filePath);
}

public sealed class ScrapeConfigurationFileReader : IScrapeConfigurationFileReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ScrapeConfigurationImportDocument> ReadAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        using var json = await JsonDocument.ParseAsync(stream);
        var document = json.RootElement.TryGetProperty("scrapeConfiguration", out _)
            ? JsonSerializer.Deserialize<ScrapeSettingsImportDocument>(json.RootElement.GetRawText(), JsonOptions)?.ToImportDocument()
            : JsonSerializer.Deserialize<ScrapeConfigurationImportDocument>(json.RootElement.GetRawText(), JsonOptions);
        if (document is null)
        {
            throw new JsonException("The configuration file is empty.");
        }
        Validate(document);
        return document;
    }

    private static void Validate(ScrapeConfigurationImportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document.UserConfiguration);
        ArgumentNullException.ThrowIfNull(document.SearchConfiguration);
        ArgumentNullException.ThrowIfNull(document.ScrapeDirectories);
        if (document.SearchConfiguration.BaseUrl is null || document.SearchConfiguration.LoginUrl is null)
        {
            throw new JsonException("The configuration must contain valid base and login URLs.");
        }
    }
}