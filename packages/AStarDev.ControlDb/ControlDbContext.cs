using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb.TagDetail;
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
/// <param name="scrapeConfigurationQuery">The query used to auto-include the sub-entities required by a <see cref="ScrapeConfigurationEntity"/> aggregate.</param>
/// <param name="filesQuery">The query used to auto-include the sub-entities required by a <see cref="FileEntity"/> aggregate.</param>
public class ControlDbContext(DbContextOptions<ControlDbContext> options, IQuery<ScrapeConfigurationEntity>? scrapeConfigurationQuery = null, IQuery<FileEntity>? filesQuery = null)
    : DbContext(options), IUnitOfWork, IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>, IRepository<FileEntity, FileId>, IRepository<TagEntity, TagId>
{
    private readonly IQuery<ScrapeConfigurationEntity> scrapeConfigurationQuery = scrapeConfigurationQuery ?? new ScrapeConfigurationQuery();
    private readonly IQuery<FileEntity> filesQuery = filesQuery ?? new FileQuery();

    /// <inheritdoc/>
    public IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>() where TAggregate : IAggregateRoot
    => (IRepository<TAggregate, TKey>)this;

    /// <inheritdoc/>
    Task<Exceptional<Option<ScrapeConfigurationEntity>>> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.TryFindAsync(ScrapeConfigurationId key) =>
        Try.RunAsync(async () => (Option<ScrapeConfigurationEntity>)await this.scrapeConfigurationQuery.Apply(ScrapeConfigurations).FirstOrDefaultAsync(scrapeConfiguration => scrapeConfiguration.Id == key));

    /// <inheritdoc/>
    Exceptional<ScrapeConfigurationEntity> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.Add(ScrapeConfigurationEntity aggregate) =>
        Try.Run(() =>
        {
            ScrapeConfigurations.Add(aggregate);
            return aggregate;
        });

    Task<Exceptional<Option<IEnumerable<ScrapeConfigurationEntity>>>> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.TryGetAllAsync()
    => Try.RunAsync(async () => (Option<IEnumerable<ScrapeConfigurationEntity>>)await this.scrapeConfigurationQuery.Apply(ScrapeConfigurations).ToListAsync());

    Task<Exceptional<Option<ScrapeConfigurationEntity>>> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.TryGetFirstAsync()
    => Try.RunAsync(async () => (Option<ScrapeConfigurationEntity>)await this.scrapeConfigurationQuery.Apply(ScrapeConfigurations).FirstOrDefaultAsync());

    /// <inheritdoc/>
    Exceptional<UnitFp> IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>.Delete(ScrapeConfigurationEntity aggregate) =>
        Try.Run(() =>
        {
            ScrapeConfigurations.Remove(aggregate);
            return UnitFp.Instance;
        });

    /// <inheritdoc/>
    Task<Exceptional<Option<FileEntity>>> IRepository<FileEntity, FileId>.TryFindAsync(FileId key) =>
        Try.RunAsync(async () => (Option<FileEntity>)await this.filesQuery.Apply(Files).FirstOrDefaultAsync(file => file.Id == key));

    /// <inheritdoc/>
    Task<Exceptional<Option<IEnumerable<FileEntity>>>> IRepository<FileEntity, FileId>.TryGetAllAsync()
    => Try.RunAsync(async () => (Option<IEnumerable<FileEntity>>)await this.filesQuery.Apply(Files).ToListAsync());

    /// <inheritdoc/>
    Task<Exceptional<Option<FileEntity>>> IRepository<FileEntity, FileId>.TryGetFirstAsync()
    => Try.RunAsync(async () => (Option<FileEntity>)await this.filesQuery.Apply(Files).FirstOrDefaultAsync());

    /// <inheritdoc/>
    Exceptional<FileEntity> IRepository<FileEntity, FileId>.Add(FileEntity aggregate) =>
        Try.Run(() =>
        {
            Files.Add(aggregate);
            return aggregate;
        });

    /// <inheritdoc/>
    Exceptional<UnitFp> IRepository<FileEntity, FileId>.Delete(FileEntity aggregate) =>
        Try.Run(() =>
        {
            Files.Remove(aggregate);
            return UnitFp.Instance;
        });

    /// <inheritdoc/>
    Task<Exceptional<Option<TagEntity>>> IRepository<TagEntity, TagId>.TryFindAsync(TagId key) =>
        Try.RunAsync(async () => (Option<TagEntity>)await Tags.FirstOrDefaultAsync(tag => tag.Id == key));

    /// <inheritdoc/>
    Task<Exceptional<Option<IEnumerable<TagEntity>>>> IRepository<TagEntity, TagId>.TryGetAllAsync()
    => Try.RunAsync(async () => (Option<IEnumerable<TagEntity>>)await Tags.ToListAsync());

    /// <inheritdoc/>
    Task<Exceptional<Option<TagEntity>>> IRepository<TagEntity, TagId>.TryGetFirstAsync()
    => Try.RunAsync(async () => (Option<TagEntity>)await Tags.FirstOrDefaultAsync());

    /// <inheritdoc/>
    Exceptional<TagEntity> IRepository<TagEntity, TagId>.Add(TagEntity aggregate) =>
        Try.Run(() =>
        {
            Tags.Add(aggregate);
            return aggregate;
        });

    /// <inheritdoc/>
    Exceptional<UnitFp> IRepository<TagEntity, TagId>.Delete(TagEntity aggregate) =>
        Try.Run(() =>
        {
            Tags.Remove(aggregate);
            return UnitFp.Instance;
        });

    /// <summary>
    /// Gets the repository for managing scrape configuration entities in the database.
    /// </summary>
    public DbSet<ScrapeConfigurationEntity> ScrapeConfigurations => Set<ScrapeConfigurationEntity>();

    public DbSet<FileEntity> Files => Set<FileEntity>();

    public DbSet<TagEntity> Tags => Set<TagEntity>();

    public DbSet<FileTagEntity> FileTags => Set<FileTagEntity>();

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

        if (!optionsBuilder.IsConfigured)
        {
            string tempDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _ = optionsBuilder.UseSqlite($"Data Source={tempDir}/Scraper/files.db");
        }

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
