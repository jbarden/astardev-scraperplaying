using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.Scoping;

/// <inheritdoc/>
public sealed class ScopedRunner(IServiceScopeFactory scopeFactory) : IScopedRunner
{
    /// <inheritdoc/>
    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> work)
        where TService : notnull
    {
        using var scope = scopeFactory.CreateScope();

        return await work(scope.ServiceProvider.GetRequiredService<TService>());
    }

    /// <inheritdoc/>
    public async Task<TResult> RunAsync<TFirst, TSecond, TResult>(Func<TFirst, TSecond, Task<TResult>> work)
        where TFirst : notnull
        where TSecond : notnull
    {
        using var scope = scopeFactory.CreateScope();

        return await work(scope.ServiceProvider.GetRequiredService<TFirst>(), scope.ServiceProvider.GetRequiredService<TSecond>());
    }
}
