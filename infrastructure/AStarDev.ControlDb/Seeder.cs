using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

public static class Seeder
{
    public static Task SeedAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        SeedImpl(context);
        return context.SaveChangesAsync(cancellationToken);
    }
    public static void Seed(DbContext context)
    {
        SeedImpl(context);
        context.SaveChanges();
    }

    private static void SeedImpl(DbContext context)
    {
        if (context.Set<ScrapeConfigurationEntity>().Any())
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Seeding ControlDbContext skipped as it already contains data.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            return;
        }

        var searchCategories = new List<SearchCategoryEntity>
                {
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category1", IsFamous = true, IncludeInSearch = true, IsInternet = false, Id = "1"},
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category2", IsFamous = false, IncludeInSearch = true, IsInternet = true, Id = "2"},
                    new(){SearchConfigurationId = new SearchConfigurationId(Guid.Empty), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Name = "category3", IsFamous = false, IncludeInSearch = true, IsInternet = false, Id = "3"}
                };
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
}
