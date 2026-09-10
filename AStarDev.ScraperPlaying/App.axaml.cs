using System.Diagnostics.CodeAnalysis;
using AStarDev.ScraperPlaying.Startup;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ApplicationMessages = AStar.Dev.Logging.Extensions.ApplicationMessages;

namespace AStarDev.ScraperPlaying;

[ExcludeFromCodeCoverage]
public partial class App : Application, IDisposable
{
    private bool disposed;
    private ServiceProvider? serviceProvider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                serviceProvider = BuildServices();
                desktop.MainWindow = serviceProvider.GetRequiredService<Home.MainWindow>();
            }
            catch (Exception exception)
            {
                desktop.MainWindow = Home.MainWindow.CreateStartupError(exception);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var configuration = ApplicationConfigurationFactory.Build(AppContext.BaseDirectory);
        var collection = new ServiceCollection().AddConfigurationServices(configuration);

        var serviceProvider = collection
            .AddDataServices()
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging()
            .BuildServiceProvider();

        var applicationDirectories = serviceProvider.GetRequiredService<IApplicationDirectories>();
        applicationDirectories.CreateIfRequired();
        var logger = serviceProvider.GetRequiredService<ILogger<App>>();
        ApplicationMessages.StartupSuccessful(logger, ApplicationMetadata.ApplicationName);
        MigrateDatabase(serviceProvider);

        return serviceProvider;
    }

    private static void MigrateDatabase(ServiceProvider serviceProvider) =>
        DatabaseMigrator.MigrateAsync(
            serviceProvider.GetRequiredService<IDbContextFactory<AStarDev.ControlDb.ControlDbContext>>(),
            serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>()).GetAwaiter().GetResult();


    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        if (disposing)
        {
            serviceProvider?.Dispose();
        }

        disposed = true;
    }

    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method - Do NOT remove this comment.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}