using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenARootDirectoryValidator
{
    private readonly MockFileSystem fileSystem = new();
    private readonly RootDirectoryValidator validator;

    public GivenARootDirectoryValidator()
    {
        fileSystem.Directory.CreateDirectory("/scrapes/root");
        fileSystem.Directory.CreateDirectory("/scrapes/famous");
        validator = new(fileSystem);
    }

    [Fact]
    public void when_both_root_directories_exist_then_there_are_no_problems()
        => validator.Validate(CreateDirectories("/scrapes/root", "/scrapes/famous")).ShouldBeEmpty();

    [Fact]
    public void when_the_root_directory_is_missing_then_it_is_reported()
        => validator.Validate(CreateDirectories("/scrapes/missing", "/scrapes/famous")).ShouldBe(["Root directory could not be found."]);

    [Fact]
    public void when_the_famous_root_directory_is_missing_on_disk_then_it_is_reported()
        => validator.Validate(CreateDirectories("/scrapes/root", "/scrapes/missing-famous")).ShouldBe(["Famous root directory could not be found."]);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_famous_root_directory_is_not_configured_then_it_is_reported_rather_than_checked_on_disk(string famousRoot)
        => validator.Validate(CreateDirectories("/scrapes/root", famousRoot)).ShouldBe(["Famous root directory is not configured."]);

    [Fact]
    public void when_both_root_directories_have_problems_then_both_are_reported()
        => validator.Validate(CreateDirectories("/scrapes/missing", "")).ShouldBe(["Root directory could not be found.", "Famous root directory is not configured."]);

    private static ScrapeDirectoriesEntity CreateDirectories(string rootDirectory, string famousRootDirectory)
        => new(new ScrapeDirectoriesId(Guid.CreateVersion7()), new ScrapeConfigurationId(Guid.CreateVersion7()), rootDirectory, famousRootDirectory, "sub");
}
