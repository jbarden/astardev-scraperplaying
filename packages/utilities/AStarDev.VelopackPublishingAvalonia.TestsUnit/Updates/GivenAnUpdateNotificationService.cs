using AStarDev.VelopackPublishing;
using AStarDev.VelopackPublishingAvalonia.Updates;
using Microsoft.Extensions.Logging.Abstractions;
using Velopack;

namespace AStarDev.VelopackPublishingAvalonia.TestsUnit.Updates;

public sealed class GivenAnUpdateNotificationService
{
    private static UpdateInfo CreateUpdateInfo(string version = "1.2.3")
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse(version), NotesMarkdown = "notes" };

        return new UpdateInfo(asset, false, null, []);
    }

    private static UpdateAvailableViewModel CreateViewModel(UpdateInfo updateInfo) =>
        new(updateInfo, new FakeVelopackUpdateService(), new FakeUpdateDialogTextProvider(), NullLogger<UpdateAvailableViewModel>.Instance);

    private static (UpdateNotificationService Sut, FakeVelopackUpdateService UpdateCheckService, FakeViewModelFactory ViewModelFactory, FakeDialogService DialogService) CreateSut()
    {
        var updateCheckService = new FakeVelopackUpdateService();
        var viewModelFactory = new FakeViewModelFactory();
        var dialogService = new FakeDialogService();
        var sut = new UpdateNotificationService(updateCheckService, viewModelFactory, dialogService);

        return (sut, updateCheckService, viewModelFactory, dialogService);
    }

    [Fact]
    public async Task when_no_update_is_available_then_dialog_is_never_shown()
    {
        var (sut, updateCheckService, viewModelFactory, dialogService) = CreateSut();
        updateCheckService.Update = null;

        await sut.CheckAndNotifyAsync(TestContext.Current.CancellationToken);

        dialogService.Shown.ShouldBeEmpty();
        viewModelFactory.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_update_is_available_then_view_model_is_created_from_the_update_info()
    {
        var (sut, updateCheckService, viewModelFactory, _) = CreateSut();
        var updateInfo = CreateUpdateInfo();
        using var viewModel = CreateViewModel(updateInfo);
        updateCheckService.Update = updateInfo;
        viewModelFactory.Factory = _ => viewModel;

        await sut.CheckAndNotifyAsync(TestContext.Current.CancellationToken);

        viewModelFactory.Created.ShouldBe([updateInfo]);
    }

    [Fact]
    public async Task when_an_update_is_available_then_dialog_is_shown_with_the_created_view_model()
    {
        var (sut, updateCheckService, viewModelFactory, dialogService) = CreateSut();
        var updateInfo = CreateUpdateInfo();
        using var viewModel = CreateViewModel(updateInfo);
        updateCheckService.Update = updateInfo;
        viewModelFactory.Factory = _ => viewModel;

        await sut.CheckAndNotifyAsync(TestContext.Current.CancellationToken);

        dialogService.Shown.Count.ShouldBe(1);
        dialogService.Shown[0].ShouldBeSameAs(viewModel);
    }

    private sealed class FakeViewModelFactory : IUpdateAvailableViewModelFactory
    {
        public Func<UpdateInfo, UpdateAvailableViewModel> Factory { get; set; } = _ => throw new InvalidOperationException("No view model configured.");

        public List<UpdateInfo> Created { get; } = [];

        public UpdateAvailableViewModel Create(UpdateInfo updateInfo)
        {
            Created.Add(updateInfo);

            return Factory(updateInfo);
        }
    }

    private sealed class FakeDialogService : IUpdateAvailableDialogService
    {
        public List<UpdateAvailableViewModel> Shown { get; } = [];

        public Task ShowAsync(UpdateAvailableViewModel viewModel, CancellationToken cancellationToken = default)
        {
            Shown.Add(viewModel);

            return Task.CompletedTask;
        }
    }
}
