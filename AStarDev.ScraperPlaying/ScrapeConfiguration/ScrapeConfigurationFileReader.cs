using System.IO.Abstractions;
using System.Text.Json;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class ScrapeConfigurationFileReader(IFileSystem fileSystem) : IScrapeConfigurationFileReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<ScrapeConfigurationImportDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = fileSystem.File.OpenRead(filePath);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
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
        if (document.BaseUrl is null || document.LoginUrl is null)
        {
            throw new JsonException("The configuration must contain valid base and login URLs.");
        }
    }
}
