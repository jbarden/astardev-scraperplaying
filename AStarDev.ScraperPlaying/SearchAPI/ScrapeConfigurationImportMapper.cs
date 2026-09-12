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
            categories);

        return new ScrapeConfigurationEntity(new ScrapeConfigurationId(document.Id))
        {
            UserConfiguration = new UserConfigurationEntity(
                new UserConfigurationId(document.UserConfiguration.Id),
                new ScrapeConfigurationId(document.Id),
                document.UserConfiguration.EmailAddress,
                document.UserConfiguration.Username,
                document.UserConfiguration.Password,
                document.UserConfiguration.ApiKey),
            SearchConfiguration = searchEntity,
            ScrapeDirectories = new ScrapeDirectoriesEntity(
                new ScrapeDirectoriesId(document.ScrapeDirectories.Id),
                new ScrapeConfigurationId(document.Id),
                document.ScrapeDirectories.RootDirectory,
                document.ScrapeDirectories.RootDirectoryFamous,
                document.ScrapeDirectories.SubDirectoryName),
            BaseUrl = document.BaseUrl,
            ApiKey = document.ApiKey,
            SearchString = document.SearchString,
            TopWallpapers = document.TopWallpapers,
            SearchStringPrefix = document.SearchStringPrefix,
            SearchStringSuffix = document.SearchStringSuffix,
            Subscriptions = document.Subscriptions,
            ImagePauseInSeconds = document.ImagePauseInSeconds,
            StartingPageNumber = document.StartingPageNumber,
            TotalPages = document.TotalPages,
            SubscriptionsStartingPageNumber = document.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages = document.SubscriptionsTotalPages,
            TopWallpapersStartingPageNumber = document.TopWallpapersStartingPageNumber,
            TopWallpapersTotalPages = document.TopWallpapersTotalPages,
            LoginUrl = document.LoginUrl,
            UseHeadless = document.UseHeadless,
            SlowMotionDelay = document.SlowMotionDelay
        };
    }
}