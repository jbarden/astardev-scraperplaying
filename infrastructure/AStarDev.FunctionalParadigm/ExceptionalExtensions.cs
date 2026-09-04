namespace AStarDev.FunctionalParadigm;

public static class ExceptionalExtensions
{
    private const string UnexpectedExceptionalTypeMessage = "Unexpected exceptional type.";

    /// <summary>
    ///     Asynchronously executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.
    /// </summary>
    public static async Task<Exceptional<T>> TapAsync<T>(this Task<Exceptional<T>> exceptionalTask, Action<T> onSuccess, Action<Exception>? onFailure = null)
    {
        var exceptional = await exceptionalTask.ConfigureAwait(false);

        return exceptional.Tap(onSuccess, onFailure);
    }

    /// <summary>
    ///     Executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.
    /// </summary>
    public static Exceptional<T> Tap<T>(this Exceptional<T> exceptional, Action<T> onSuccess, Action<Exception>? onFailure = null)
    {
        switch (exceptional)
        {
            case Success<T> success:
                onSuccess(success.Value);

                return success;

            case Failure<T> failure:
                onFailure?.Invoke(failure.Exception);

                return failure;

            default:
                throw new InvalidOperationException(UnexpectedExceptionalTypeMessage);
        }
    }
}