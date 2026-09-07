namespace AStarDev.ScraperPlaying.SearchAPI;

internal static class ScrapeSettingsImportMapper
{
    public static ScrapeConfigurationImportDocument ToImportDocument(this ScrapeSettingsImportDocument source)
    {
        var configurationId = Guid.CreateVersion7();
        var searchConfigurationId = Guid.CreateVersion7();
        var userConfiguration = source.ScrapeConfiguration.UserConfiguration;
        var searchConfiguration = source.ScrapeConfiguration.SearchConfiguration;
        var directories = source.ScrapeConfiguration.ScrapeDirectories;

        return new ScrapeConfigurationImportDocument
        {
            Id = configurationId,
            UserConfiguration = new UserConfigurationImportDocument
            {
                Id = Guid.CreateVersion7(),
                EmailAddress = userConfiguration.LoginEmailAddress,
                Username = userConfiguration.Username,
                Password = userConfiguration.Password
            },
            SearchConfiguration = new SearchConfigurationImportDocument
            {
                Id = searchConfigurationId,
                BaseUrl = searchConfiguration.BaseUrl,
                LoginUrl = searchConfiguration.LoginUrl,
                SearchCategories = searchConfiguration.SearchCategories,
                SearchString = searchConfiguration.SearchString,
                TopWallpapers = searchConfiguration.TopWallpapers,
                SearchStringPrefix = searchConfiguration.SearchStringPrefix,
                SearchStringSuffix = searchConfiguration.SearchStringSuffix,
                Subscriptions = searchConfiguration.Subscriptions,
                ImagePauseInSeconds = searchConfiguration.ImagePauseInSeconds,
                StartingPageNumber = searchConfiguration.StartingPageNumber,
                TotalPages = searchConfiguration.TotalPages,
                UseHeadless = searchConfiguration.UseHeadless,
                SlowMotionDelay = searchConfiguration.SlowMotionDelay,
                SubscriptionsStartingPageNumber = searchConfiguration.SubscriptionsStartingPageNumber,
                SubscriptionsTotalPages = searchConfiguration.SubscriptionsTotalPages,
                TopWallpapersTotalPages = searchConfiguration.TopWallpapersTotalPages,
                TopWallpapersStartingPageNumber = searchConfiguration.TopWallpapersStartingPageNumber
            },
            ScrapeDirectories = new ScrapeDirectoriesImportDocument
            {
                Id = Guid.CreateVersion7(),
                RootDirectory = directories.BaseDirectory,
                BaseSaveDirectory = directories.BaseSaveDirectory,
                BaseDirectory = directories.BaseDirectory,
                BaseDirectoryFamous = directories.BaseDirectoryFamous,
                SubDirectoryName = directories.SubDirectoryName
            }
        };
    }
}