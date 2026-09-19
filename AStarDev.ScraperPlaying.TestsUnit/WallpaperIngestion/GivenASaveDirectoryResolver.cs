using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenASaveDirectoryResolver
{
    private readonly IScrapeDirectoriesQuery directoriesQuery = Substitute.For<IScrapeDirectoriesQuery>();
    private readonly MockFileSystem fileSystem = new();
    private readonly SaveDirectoryResolver resolver;

    public GivenASaveDirectoryResolver()
    {
        directoriesQuery.GetAsync(Arg.Any<CancellationToken>()).Returns(Stored("root-directory", "famous-directory"));
        resolver = new(fileSystem, directoriesQuery);
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
    public async Task when_several_directories_are_resolved_then_the_configuration_is_loaded_only_once()
    {
        var loadCount = 0;
        directoriesQuery.GetAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            loadCount++;

            return Stored("root-directory", "famous-directory");
        });

        await resolver.ResolveSaveDirectoryAsync(Option.None<string>(), false, CancellationToken.None);
        await resolver.ResolveSaveDirectoryAsync(Option.Some("My Category"), true, CancellationToken.None);
        await resolver.ResolveSaveDirectoryAsync(Option.Some("Other"), false, CancellationToken.None);

        loadCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_no_scrape_configuration_exists_then_it_throws()
    {
        directoriesQuery.GetAsync(Arg.Any<CancellationToken>()).Returns((Exceptional<Option<ScrapeDirectoriesEntity>>)Option.None<ScrapeDirectoriesEntity>());

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
        directoriesQuery.GetAsync(Arg.Any<CancellationToken>()).Returns(Stored("root-directory", famousRoot));

        await Should.ThrowAsync<InvalidOperationException>(
            () => resolver.ResolveSaveDirectoryAsync(Option.None<string>(), true, CancellationToken.None));
    }

    private static Exceptional<Option<ScrapeDirectoriesEntity>> Stored(string rootDirectory, string famousRootDirectory)
        => (Option<ScrapeDirectoriesEntity>)new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), new ScrapeConfigurationId(Guid.CreateVersion7()), rootDirectory, famousRootDirectory, "");
}
