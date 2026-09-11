using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.Utilities;
using AStarDev.ScraperPlaying.SearchAPI;

namespace AStarDev.ScraperPlaying.Startup;

public static class DataServices
{
    /// <summary>Registers the scrape configuration repository with the dependency injection container.</summary>
    /// <param name="services">The service collection to register the data services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        string dbPath = BuildDbPath();

        _ = services.AddScoped<IScrapeConfigurationImporter, ScrapeConfigurationImporter>()
            .AddScoped<IQuery<ScrapeConfigurationEntity>, ScrapeConfigurationQuery>()
            .AddScoped<IQuery<FileEntity>, FileQuery>()
            .AddScoped<IUnitOfWork, ControlDbContext>()
            .AddScoped<IFilesQuery, FilesQuery>()
            .AddScoped<ITagsQuery, TagsQuery>()
            .AddScoped<IFileTagRepository, FileTagRepository>()
            .AddDbContextFactory<ControlDbContext>((serviceProvider, options) => options.UseSqlite($"Data Source={dbPath}"));

        return services;
    }

    private static string BuildDbPath()
        => ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory()
                .CombinePath(Path.DirectorySeparatorChar.ToString())
                .CombinePath("data")
                .CombinePath(Path.DirectorySeparatorChar.ToString())
                .CombinePath("astar-control.db");
}
