using AStarDev.VelopackPublishing;
using Velopack;

namespace AStarDev.VelopackPublishingAvalonia.TestsUnit.Updates;

/// <summary>An update service whose result is set by the test and which records the order of the download and apply calls.</summary>
internal sealed class FakeVelopackUpdateService : IVelopackUpdateService
{
    public bool IsInstalled => true;

    public string Channel => "test";

    /// <summary>The update returned by <see cref="CheckForUpdatesAsync"/>; <see langword="null"/> means no update is available (the interface's own contract).</summary>
    public UpdateInfo? Update { get; set; }

    /// <summary>The failure thrown by <see cref="DownloadUpdatesAsync"/>, or <see langword="null"/> for it to succeed.</summary>
    public Exception? DownloadFailure { get; set; }

    /// <summary>The ordered log of calls: <c>download:{version}</c> and <c>apply:{version}</c>.</summary>
    public List<string> Operations { get; } = [];

    public Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default) => Task.FromResult(Update);

    public Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (DownloadFailure is not null) return Task.FromException(DownloadFailure);

        Operations.Add($"download:{updateInfo.TargetFullRelease.Version}");

        return Task.CompletedTask;
    }

    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo) => Operations.Add($"apply:{updateInfo.TargetFullRelease.Version}");
}
