using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.EFCoreSqlite;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>
/// Represents the Entity Framework database context for managing file entities.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ControlDbContext"/> class with the specified options.
/// </remarks>
/// <param name="options">The options to be used by the DbContext.</param>
public class ControlDbContext(DbContextOptions<ControlDbContext> options) : DbContext(options), IUnitOfWork, IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>
{
    /// <inheritdoc/>
    public IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>() where TAggregate : IAggregateRoot
    => (IRepository<TAggregate, TKey>)this;

    /// <inheritdoc/>
    Task<Exceptional<Option<ScrapeConfigurationEntity>>> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.TryFindAsync(ScrapeConfigurationId key) =>
        Try.RunAsync(async () => (Option<ScrapeConfigurationEntity>)await ScrapeConfigurations.FindAsync(key));

    /// <inheritdoc/>
    Exceptional<ScrapeConfigurationEntity> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.Add(ScrapeConfigurationEntity aggregate) =>
        Try.Run(() =>
        {
            ScrapeConfigurations.Add(aggregate);
            return aggregate;
        });

    Task<Exceptional<Option<IEnumerable<ScrapeConfigurationEntity>>>> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.TryGetAllAsync()
    => Try.RunAsync(async () => (Option<IEnumerable<ScrapeConfigurationEntity>>)(await ScrapeConfigurations.ToListAsync()));

    Task<Exceptional<Option<ScrapeConfigurationEntity>>> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.TryGetFirstAsync()
    => Try.RunAsync(async () => (Option<ScrapeConfigurationEntity>)(await ScrapeConfigurations.FirstOrDefaultAsync()));

    /// <inheritdoc/>
    Exceptional<UnitFp> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.Delete(ScrapeConfigurationEntity aggregate) =>
        Try.Run(() =>
        {
            ScrapeConfigurations.Remove(aggregate);
            return UnitFp.Instance;
        });

    /// <summary>
    ///   Gets the repository for managing file entities in the database.
    /// </summary>
    public ControlDbContext() : this(new DbContextOptions<ControlDbContext>()) { }

    /// <summary>
    /// Gets the repository for managing scrape configuration entities in the database.
    /// </summary>
    public DbSet<ScrapeConfigurationEntity> ScrapeConfigurations => Set<ScrapeConfigurationEntity>();

    public DbSet<FileEntity> Files => Set<FileEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(ControlDbContext).Assembly);

        modelBuilder.UseSqliteFriendlyConversions();
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<string>().UseCollation("NOCASE");

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        string tempDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!optionsBuilder.IsConfigured) _ = optionsBuilder.UseSqlite($"Data Source={tempDir}/Scraper/files.db");

        optionsBuilder
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                await Seeder.SeedAsync(context, cancellationToken);
                Console.WriteLine("Async Seeding ControlDbContext completed successfully...");
            })
            .UseSeeding((context, _) =>
            {
                Seeder.Seed(context);
                Console.WriteLine("Sync Seeding ControlDbContext completed successfully...");
            });
    }
}
