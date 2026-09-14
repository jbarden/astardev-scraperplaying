using AStarDev.VelopackPublishingAvalonia.TestsUnit.Controls;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace AStarDev.VelopackPublishingAvalonia.TestsUnit.Controls;

internal sealed class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
