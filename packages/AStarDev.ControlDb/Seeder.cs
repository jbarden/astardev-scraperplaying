using System.Text.Json;
using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Provides methods for seeding the database with initial data.</summary>
public static class Seeder
{
    private const string DefaultBackupSearchCategoriesFilePath = "/run/media/jbarden/Tbdrive/backup-files/SearchCategories.json";

    /// <summary>Seeds the database with initial data asynchronously.</summary>
    /// <param name="context">The database context used for seeding data.</param>
    /// <param name="backupSearchCategoriesFilePath">The path to an optional backup file of search categories to seed from.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    public static Task SeedAsync(DbContext context, string backupSearchCategoriesFilePath = DefaultBackupSearchCategoriesFilePath, CancellationToken cancellationToken = default)
    {
        SeedImpl(context, backupSearchCategoriesFilePath);

        return context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Seeds the database with initial data synchronously.</summary>
    /// <param name="context">The database context used for seeding data.</param>
    /// <param name="backupSearchCategoriesFilePath">The path to an optional backup file of search categories to seed from.</param>
    public static void Seed(DbContext context, string backupSearchCategoriesFilePath = DefaultBackupSearchCategoriesFilePath)
    {
        SeedImpl(context, backupSearchCategoriesFilePath);
        context.SaveChanges();
    }

    private static void SeedImpl(DbContext context, string backupSearchCategoriesFilePath)
    {
        if (context.Set<ScrapeConfigurationEntity>().Any())
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Seeding ControlDbContext skipped as it already contains data.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            return;
        }

        var searchCategories = LoadSearchCategoriesFromBackup(backupSearchCategoriesFilePath) ?? DefaultSearchCategories();

        context.Set<ScrapeConfigurationEntity>().Add(new ScrapeConfigurationEntity(Guid.Empty)
        {
            UserConfiguration = new UserConfigurationEntity(Guid.Empty, Guid.Empty, "jason.j.barden2@outlook.com", "jbarden", Environment.GetEnvironmentVariable("ScrapePassword") ?? "Password1!", "n/a"),
            SearchConfiguration = new SearchConfigurationEntity(Guid.Empty, Guid.Empty, "search term", 500, searchCategories),
            ScrapeDirectories = new ScrapeDirectoriesEntity(Guid.Empty, Guid.Empty, "/run/media/jbarden/Tbdrive/sync/jason.barden1@outlook.com/", "Pictures/WallHaven/Famous", "subdirectory"),
            BaseUrl = new Uri("https://wallhaven.cc/"),
            ApiKey = "your-api-key",
            SearchString = "search string",
            TopWallpapers = "top wallpapers",
            SearchStringPrefix = "/search?q=id:",
            SearchStringSuffix = "&categories=001&purity=111&sorting=date_added&order=desc&ai_art_filter=0&page=",
            SlowMotionDelay = 500,
            Subscriptions = "subscription?page=",
            UseHeadless = false
        });
    }

    private static List<SearchCategoryEntity>? LoadSearchCategoriesFromBackup(string backupSearchCategoriesFilePath)
    {
        if (!File.Exists(backupSearchCategoriesFilePath))
            return null;

        try
        {
            var searchCategoriesJson = File.ReadAllText(backupSearchCategoriesFilePath);

            return JsonSerializer.Deserialize<List<SearchCategoryEntity>>(searchCategoriesJson);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine($"Backup file for search categories at '{backupSearchCategoriesFilePath}' could not be read ({exception.Message}) - added defaults.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            return null;
        }
    }

    private static List<SearchCategoryEntity> DefaultSearchCategories()
    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine("Backup file for search categories not found - added defaults.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

        return
        [
            new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category1", IsFamous = true, IncludeInSearch = true, IsInternet = false, Id = "1"},
            new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category2", IsFamous = false, IncludeInSearch = true, IsInternet = true, Id = "2"},
            new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category3", IsFamous = false, IncludeInSearch = true, IsInternet = false, Id = "3"}
        ];
    }
}
