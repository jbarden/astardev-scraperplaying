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
    /// <param name="databasePath">
    /// The SQLite database file path. Defaults to the real per-user application data location; overridable so
    /// tests can point at a temp file instead of the real user database.
    /// </param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services, string? databasePath = null)
    {
        string dbPath = databasePath ?? BuildDbPath();

        _ = services.AddScoped<IScrapeConfigurationImporter, ScrapeConfigurationImporter>()
            .AddScoped<IQuery<ScrapeConfigurationEntity>, ScrapeConfigurationQuery>()
            .AddScoped<IQuery<FileEntity>, FileQuery>()
            .AddScoped<IUnitOfWork, ControlDbContext>()
            .AddScoped<IFilesQuery, FilesQuery>()
            .AddScoped<ITagsQuery, TagsQuery>()
            .AddScoped<IFileTagRepository, FileTagRepository>()
            .AddDbContextFactory<ControlDbContext>((serviceProvider, options) => options.UseSqlite($"Data Source={dbPath}"))
            // AddDbContextFactory also registers ControlDbContext itself as its own scoped service, independent
            // of the IUnitOfWork registration above - within one scope that would otherwise resolve a *second*,
            // never-saved ControlDbContext instance for anything (FilesQuery, TagsQuery, FileTagRepository) that
            // takes ControlDbContext directly. Registering it last makes every direct ControlDbContext resolution
            // reuse the same instance IUnitOfWork resolves.
            .AddScoped(serviceProvider => (ControlDbContext)serviceProvider.GetRequiredService<IUnitOfWork>());

        return services;
    }

    private static string BuildDbPath()
        => ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory()
                .CombinePath(Path.DirectorySeparatorChar.ToString())
                .CombinePath("data")
                .CombinePath(Path.DirectorySeparatorChar.ToString())
                .CombinePath("astar-control.db");
}
