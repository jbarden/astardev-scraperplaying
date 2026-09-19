using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationTransferService
{
    private readonly IScrapeConfigurationImportService importService = Substitute.For<IScrapeConfigurationImportService>();
    private readonly IScrapeConfigurationExportService exportService = Substitute.For<IScrapeConfigurationExportService>();
    private readonly ScrapeConfigurationTransferService service;

    public GivenAScrapeConfigurationTransferService()
        => service = new(new ServiceCollection().AddScoped(_ => importService).AddScoped(_ => exportService).BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());

    [Fact]
    public async Task when_a_file_is_imported_then_the_import_service_imports_it()
    {
        await service.ImportAsync("path/to/import.json", TestContext.Current.CancellationToken);

        await importService.Received(1).ImportAsync("path/to/import.json", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task when_a_configuration_is_exported_then_whether_one_existed_is_returned(bool exists)
    {
        exportService.ExportAsync("path/to/export.json", Arg.Any<CancellationToken>()).Returns(exists);

        var exported = await service.ExportAsync("path/to/export.json", TestContext.Current.CancellationToken);

        exported.ShouldBe(exists);
    }

    [Fact]
    public async Task when_the_import_fails_then_the_failure_propagates()
    {
        importService.ImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new IOException("boom"));

        await Should.ThrowAsync<IOException>(() => service.ImportAsync("path/to/import.json", TestContext.Current.CancellationToken));
    }
}
