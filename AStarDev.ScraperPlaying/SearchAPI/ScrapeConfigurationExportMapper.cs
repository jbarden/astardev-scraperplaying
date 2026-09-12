using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.SearchAPI;

public static class ScrapeConfigurationExportMapper
{
    public static ScrapeConfigurationImportDocument ToImportDocument(this ScrapeConfigurationEntity entity)
    {
        var search = entity.SearchConfiguration;

        return new ScrapeConfigurationImportDocument
        {
            Id = entity.Id.Value,
            UserConfiguration = new UserConfigurationImportDocument
            {
                Id = entity.UserConfiguration.Id.Value,
                EmailAddress = entity.UserConfiguration.EmailAddress,
                Username = entity.UserConfiguration.Username,
                Password = entity.UserConfiguration.Password,
                ApiKey = entity.UserConfiguration.ApiKey
            },
            SearchConfiguration = new SearchConfigurationImportDocument
            {
                Id = search.Id.Value,
                SearchTerm = search.SearchTerm,
                MaxResults = search.MaxResults,
                BaseUrl = search.BaseUrl,
                ApiKey = search.ApiKey,
                SearchString = search.SearchString,
                TopWallpapers = search.TopWallpapers,
                SearchStringPrefix = search.SearchStringPrefix,
                SearchStringSuffix = search.SearchStringSuffix,
                Subscriptions = search.Subscriptions,
                ImagePauseInSeconds = search.ImagePauseInSeconds,
                StartingPageNumber = search.StartingPageNumber,
                TotalPages = search.TotalPages,
                SubscriptionsStartingPageNumber = search.SubscriptionsStartingPageNumber,
                SubscriptionsTotalPages = search.SubscriptionsTotalPages,
                TopWallpapersStartingPageNumber = search.TopWallpapersStartingPageNumber,
                TopWallpapersTotalPages = search.TopWallpapersTotalPages,
                LoginUrl = search.LoginUrl,
                UseHeadless = search.UseHeadless,
                SlowMotionDelay = search.SlowMotionDelay,
                SearchCategories = search.SearchCategories.Select(category => new SearchCategoryImportDocument
                {
                    Id = category.Id,
                    Name = category.Name,
                    LastKnownImageCount = category.LastKnownImageCount,
                    LastPageVisited = category.LastPageVisited,
                    TotalPages = category.TotalPages,
                    IncludeInSearch = category.IncludeInSearch,
                    IsFamous = category.IsFamous,
                    IsInternet = category.IsInternet
                }).ToList()
            },
            ScrapeDirectories = new ScrapeDirectoriesImportDocument
            {
                Id = entity.ScrapeDirectories.Id.Value,
                RootDirectory = entity.ScrapeDirectories.RootDirectory,
                RootDirectoryFamous = entity.ScrapeDirectories.RootDirectoryFamous,
                SubDirectoryName = entity.ScrapeDirectories.SubDirectoryName
            }
        };
    }
}
