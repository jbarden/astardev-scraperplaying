using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenASaveDirectoryResolver
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> scrapeConfigurationRepository = Substitute.For<IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>>();
    private readonly MockFileSystem fileSystem = new();
    private readonly SaveDirectoryResolver resolver;

    public GivenASaveDirectoryResolver()
    {
        unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().Returns(scrapeConfigurationRepository);
        scrapeConfigurationRepository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)CreateConfiguration("root-directory", "famous-directory"));
        resolver = new(fileSystem, unitOfWork);
    }

    [Fact]
    public async Task when_no_category_name_is_supplied_then_the_resolved_directory_is_the_root_combined_with_top_wallpapers()
    {
        var directory = await resolver.ResolveSaveDirectoryAsync(Option.None<string>(), false, CancellationToken.None);

        directory.ShouldBe(fileSystem.Path.Combine("root-directory", "top-wallpapers"));
    }

    [Fact]
    public async Task when_a_category_name_is_supplied_then_the_resolved_directory_is_the_root_combined_with_the_slugified_category_name()
    {
        var directory = await resolver.ResolveSaveDirectoryAsync(Option.Some("My Category"), false, CancellationToken.None);

        directory.ShouldBe(fileSystem.Path.Combine("root-directory", "my-category"));
    }

    [Fact]
    public async Task when_no_scrape_configuration_exists_then_it_throws()
    {
        scrapeConfigurationRepository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)Option.None<ScrapeConfigurationEntity>());

        await Should.ThrowAsync<InvalidOperationException>(
            () => resolver.ResolveSaveDirectoryAsync(Option.None<string>(), false, CancellationToken.None));
    }

    [Fact]
    public async Task when_the_wallpaper_is_famous_then_the_resolved_directory_uses_the_famous_root_combined_with_the_category()
    {
        var directory = await resolver.ResolveSaveDirectoryAsync(Option.Some("My Category"), true, CancellationToken.None);

        directory.ShouldBe(fileSystem.Path.Combine("famous-directory", "my-category"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task when_the_wallpaper_is_famous_and_no_famous_root_is_configured_then_it_throws_rather_than_using_the_root(string famousRoot)
    {
        scrapeConfigurationRepository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)CreateConfiguration("root-directory", famousRoot));

        await Should.ThrowAsync<InvalidOperationException>(
            () => resolver.ResolveSaveDirectoryAsync(Option.None<string>(), true, CancellationToken.None));
    }

    private static ScrapeConfigurationEntity CreateConfiguration(string rootDirectory, string famousRootDirectory)
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, rootDirectory, famousRootDirectory, "")
        };
    }
}
