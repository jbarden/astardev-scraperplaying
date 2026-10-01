using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenASaveDirectoryResolver
{
    private static readonly Tag FamousTag = new(1, "Famous Person", "famous-person", 1, "Celebrities", "sfw", IsFamous: true);
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> scrapeConfigurationRepository;
    private readonly SaveDirectoryResolver resolver;

    public GivenASaveDirectoryResolver()
    {
        scrapeConfigurationRepository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        scrapeConfigurationRepository.First = (Option<ScrapeConfigurationEntity>)CreateConfiguration("root-directory");
        resolver = new(unitOfWork);
    }

    [Fact]
    public async Task when_no_category_name_is_supplied_then_the_resolved_directory_is_the_root_combined_with_top_wallpapers()
    {
        var directories = await resolver.ResolveSaveDirectoriesAsync(Option.None<string>(), CancellationToken.None);

        directories.For([]).ShouldBe(Path.Combine("root-directory", "top-wallpapers"));
    }

    [Fact]
    public async Task when_the_hot_wallpapers_name_is_supplied_then_the_resolved_directory_is_the_root_combined_with_hot_wallpapers()
    {
        var directories = await resolver.ResolveSaveDirectoriesAsync(Option.Some("Hot Wallpapers"), CancellationToken.None);

        directories.For([]).ShouldBe(Path.Combine("root-directory", "hot-wallpapers"));
    }

    [Fact]
    public async Task when_a_category_name_is_supplied_then_the_resolved_directory_is_the_root_combined_with_the_slugified_category_name()
    {
        var directories = await resolver.ResolveSaveDirectoriesAsync(Option.Some("My Category"), CancellationToken.None);

        directories.For([]).ShouldBe(Path.Combine("root-directory", "my-category"));
    }

    [Fact]
    public async Task when_a_category_name_is_supplied_then_the_famous_directory_is_the_famous_root_combined_with_the_slugified_category_name()
    {
        var directories = await resolver.ResolveSaveDirectoriesAsync(Option.Some("My Category"), CancellationToken.None);

        directories.For([FamousTag]).ShouldBe(Path.Combine("famous-root-directory", "famous-person", "my-category"));
    }

    [Fact]
    public async Task when_no_category_name_is_supplied_then_the_famous_directory_is_the_famous_root_combined_with_top_wallpapers()
    {
        var directories = await resolver.ResolveSaveDirectoriesAsync(Option.None<string>(), CancellationToken.None);

        directories.For([FamousTag]).ShouldBe(Path.Combine("famous-root-directory", "famous-person", "top-wallpapers"));
    }

    [Fact]
    public async Task when_the_directory_is_resolved_again_then_the_scrape_configuration_is_not_reloaded()
    {
        _ = await resolver.ResolveSaveDirectoriesAsync(Option.None<string>(), CancellationToken.None);
        scrapeConfigurationRepository.First = (Option<ScrapeConfigurationEntity>)CreateConfiguration("changed-root-directory");

        var directories = await resolver.ResolveSaveDirectoriesAsync(Option.Some("My Category"), CancellationToken.None);

        directories.For([]).ShouldBe(Path.Combine("root-directory", "my-category"));
    }

    [Fact]
    public async Task when_loading_the_root_directory_failed_then_the_next_resolve_loads_it_again()
    {
        scrapeConfigurationRepository.First = Option<ScrapeConfigurationEntity>.None.Instance;
        _ = await Should.ThrowAsync<InvalidOperationException>(() => resolver.ResolveSaveDirectoriesAsync(Option.None<string>(), CancellationToken.None));
        scrapeConfigurationRepository.First = (Option<ScrapeConfigurationEntity>)CreateConfiguration("recovered-root-directory");

        var directories = await resolver.ResolveSaveDirectoriesAsync(Option.None<string>(), CancellationToken.None);

        directories.For([]).ShouldBe(Path.Combine("recovered-root-directory", "top-wallpapers"));
    }

    [Fact]
    public async Task when_no_scrape_configuration_exists_then_it_throws()
    {
        scrapeConfigurationRepository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        await Should.ThrowAsync<InvalidOperationException>(
            () => resolver.ResolveSaveDirectoriesAsync(Option.None<string>(), CancellationToken.None));
    }

    private static ScrapeConfigurationEntity CreateConfiguration(string rootDirectory)
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, rootDirectory, $"famous-{rootDirectory}", "")
        };
    }
}
