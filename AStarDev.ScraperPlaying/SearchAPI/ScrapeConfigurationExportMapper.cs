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
            },
            BaseUrl = entity.BaseUrl,
            ApiKey = entity.ApiKey,
            SearchString = entity.SearchString,
            TopWallpapers = entity.TopWallpapers,
            SearchStringPrefix = entity.SearchStringPrefix,
            SearchStringSuffix = entity.SearchStringSuffix,
            Subscriptions = entity.Subscriptions,
            ImagePauseInSeconds = entity.ImagePauseInSeconds,
            StartingPageNumber = entity.StartingPageNumber,
            TotalPages = entity.TotalPages,
            SubscriptionsStartingPageNumber = entity.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages = entity.SubscriptionsTotalPages,
            TopWallpapersStartingPageNumber = entity.TopWallpapersStartingPageNumber,
            TopWallpapersTotalPages = entity.TopWallpapersTotalPages,
            LoginUrl = entity.LoginUrl,
            UseHeadless = entity.UseHeadless,
            SlowMotionDelay = entity.SlowMotionDelay
        };
    }
}
