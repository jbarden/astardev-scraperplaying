using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenARootDirectoryCheck : IDisposable
{
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;
    private readonly MockFileSystem fileSystem = new();
    private readonly ServiceProvider serviceProvider;
    private readonly RootDirectoryCheck check;

    public GivenARootDirectoryCheck()
    {
        repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        serviceProvider = new ServiceCollection().AddSingleton<IUnitOfWork>(unitOfWork).BuildServiceProvider();
        check = new(serviceProvider.GetRequiredService<IServiceScopeFactory>(), fileSystem);
    }

    public void Dispose() => serviceProvider.Dispose();

    [Fact]
    public async Task when_the_root_directory_exists_on_disk_then_it_exists()
    {
        fileSystem.Directory.CreateDirectory("/scrapes/root");
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration(rootDirectory: "/scrapes/root");

        var exists = await check.ExistsAsync();

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_root_directory_does_not_exist_on_disk_then_it_does_not_exist()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration(rootDirectory: "/scrapes/missing");

        var exists = await check.ExistsAsync();

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task when_no_configuration_exists_then_it_throws()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        _ = await Should.ThrowAsync<InvalidOperationException>(() => check.ExistsAsync());
    }
}
