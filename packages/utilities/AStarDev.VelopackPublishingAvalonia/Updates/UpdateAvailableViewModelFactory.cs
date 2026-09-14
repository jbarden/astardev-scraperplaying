using AStarDev.VelopackPublishing;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStarDev.VelopackPublishingAvalonia.Updates;

/// <inheritdoc cref="IUpdateAvailableViewModelFactory" />
public sealed class UpdateAvailableViewModelFactory(IVelopackUpdateService updateCheckService, IUpdateDialogTextProvider textProvider, ILogger<UpdateAvailableViewModel> logger) : IUpdateAvailableViewModelFactory
{
    /// <inheritdoc />
    public UpdateAvailableViewModel Create(UpdateInfo updateInfo) => new(updateInfo, updateCheckService, textProvider, logger);
}
