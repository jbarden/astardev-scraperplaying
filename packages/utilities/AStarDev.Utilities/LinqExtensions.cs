namespace AStarDev.Utilities;

/// <summary>
/// </summary>
public static class LinqExtensions
{
    extension<T>(IEnumerable<T> enumerable)
    {
        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        public void ForEach(Action<T> action)
        {
            if (enumerable == null || action == null) return;

            foreach (var item in enumerable) action(item);
        }

        /// <summary>
        ///     Awaits the supplied asynchronous action once per item, strictly sequentially, so no two invocations
        ///     ever run concurrently.
        /// </summary>
        /// <param name="action"></param>
        public async Task ForEachAsync(Func<T, Task> action)
        {
            foreach (var item in enumerable)
                await action(item).ConfigureAwait(false);
        }
    }
}
