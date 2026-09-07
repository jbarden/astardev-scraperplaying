using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ControlDb.TestsIntegration.TestDataFactories;

internal static class FileDetailEntityFactory
{
    public static FileDetailEntity CreateFileDetailEntity()
    {
        var fileDetailId = new FileDetailId(Guid.Empty);
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.Empty);
        var fileDetail = new FileDetailEntity(fileDetailId, scrapeConfigurationId, "file-name", "file-path", "file-type");

        return fileDetail;
    }
}