namespace AStarDev.FunctionalParadigm;

public static class OptionExtensions
{
    /// <summary>
    ///     Awaits the <see cref="Option{T}" />-producing task, then pattern matches on the result.
    /// </summary>
    public static async Task<TResult> MatchAsync<T, TResult>(this Task<Option<T>> optionTask, Func<T, Task<TResult>> onSome, Func<TResult> onNone)
    {
        var option = await optionTask.ConfigureAwait(false);

        return await option.MatchAsync(onSome, onNone).ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the <see cref="Option{T}" />-producing task, then pattern matches on the result for side effects.
    /// </summary>
    public static async Task MatchAsync<T>(this Task<Option<T>> optionTask, Func<T, Task> onSome, Action onNone)
    {
        var option = await optionTask.ConfigureAwait(false);

        await option.MatchAsync(onSome, onNone).ConfigureAwait(false);
    }
}
