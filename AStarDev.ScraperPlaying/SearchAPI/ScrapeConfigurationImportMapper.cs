using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.SearchAPI;

public static class ScrapeConfigurationImportMapper
{
    public static ScrapeConfigurationEntity ToEntity(this ScrapeConfigurationImportDocument document)
    {
        var search = document.SearchConfiguration;
        var categories = search.SearchCategories.Select(category => new SearchCategoryEntity
        {
            SearchConfigurationId = new SearchConfigurationId(search.Id),
            Id = category.Id,
            Name = category.Name,
            LastKnownImageCount = category.LastKnownImageCount,
            LastPageVisited = category.LastPageVisited,
            TotalPages = category.TotalPages,
            IncludeInSearch = category.IncludeInSearch,
            IsFamous = category.IsFamous,
            IsInternet = category.IsInternet,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        var searchEntity = new SearchConfigurationEntity(
            new SearchConfigurationId(search.Id),
            new ScrapeConfigurationId(document.Id),
            search.SearchTerm,
            search.MaxResults ?? 0,
            categories)
        {
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
            SlowMotionDelay = search.SlowMotionDelay
        };

        return new ScrapeConfigurationEntity(new ScrapeConfigurationId(document.Id))
        {
            UserConfiguration = new UserConfigurationEntity(
                new UserConfigurationId(document.UserConfiguration.Id),
                new ScrapeConfigurationId(document.Id),
                document.UserConfiguration.EmailAddress,
                document.UserConfiguration.Username,
                document.UserConfiguration.Password,
                document.UserConfiguration.SessionCookie),
            SearchConfiguration = searchEntity,
            ScrapeDirectories = new ScrapeDirectoriesEntity(
                new ScrapeDirectoriesId(document.ScrapeDirectories.Id),
                new ScrapeConfigurationId(document.Id),
                document.ScrapeDirectories.RootDirectory,
                document.ScrapeDirectories.BaseSaveDirectory,
                document.ScrapeDirectories.BaseDirectory,
                document.ScrapeDirectories.BaseDirectoryFamous,
                document.ScrapeDirectories.SubDirectoryName)
        };
    }
}