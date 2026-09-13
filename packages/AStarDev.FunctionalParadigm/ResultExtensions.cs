using AStarDev.FunctionalParadigm;

namespace AStarDev.FunctionalParadigm;

public static class ResultExtensions
{
    private const string UnexpectedResultTypeMessage = "Unexpected result type.";

    extension<TResult, TError>(Result<TResult, TError> result)
    {
        public Result<TResult, TError> Tap(Action<TResult> onSuccess, Action<TError>? onFailure = null)
        {
            switch (result)
            {
                case Ok<TResult, TError> ok:
                    onSuccess(ok.Value);
                    return ok;

                case Fail<TResult, TError> fail:
                    onFailure?.Invoke(fail.Error);
                    return fail;

                default:
                    throw new InvalidOperationException(UnexpectedResultTypeMessage);
            }
        }

        public Result<TResult, TError> TapError(Action<TError> onFailure)
        {
            if (result is Fail<TResult, TError> fail) onFailure(fail.Error);

            return result;
        }

        public async Task<Result<TResult, TError>> TapAsync(Func<TResult, Task> onSuccessAsync)
        {
            if (result is Ok<TResult, TError> ok) await onSuccessAsync(ok.Value).ConfigureAwait(false);

            return result;
        }

        public Result<TMapped, TError> Map<TMapped>(Func<TResult, TMapped> selector)
            => result switch
            {
                Ok<TResult, TError> ok => new Ok<TMapped, TError>(selector(ok.Value)),
                Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

        public async Task<Result<TMapped, TError>> MapAsync<TMapped>(Func<TResult, Task<TMapped>> selector) => result switch
        {
            Ok<TResult, TError> ok => new Ok<TMapped, TError>(await selector(ok.Value).ConfigureAwait(false)),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
        };

        public async ValueTask<Result<TMapped, TError>> MapAsync<TMapped>(Func<TResult, ValueTask<TMapped>> selector) => result switch
        {
            Ok<TResult, TError> ok => new Ok<TMapped, TError>(await selector(ok.Value).ConfigureAwait(false)),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
        };

        public async Task<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, Task<Result<TMapped, TError>>> binder) => result switch
        {
            Ok<TResult, TError> ok => await binder(ok.Value).ConfigureAwait(false),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
        };

        public async ValueTask<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, ValueTask<Result<TMapped, TError>>> binder) => result switch
        {
            Ok<TResult, TError> ok => await binder(ok.Value).ConfigureAwait(false),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
        };

        public Result<TMapped, TError> Bind<TMapped>(Func<TResult, Result<TMapped, TError>> binder)
            => result switch
            {
                Ok<TResult, TError> ok => binder(ok.Value),
                Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

        public Result<TResult, TError> Ensure(Action finallyAction)
        {
            finallyAction();

            return result;
        }

        public TOut Match<TOut>(Func<TResult, TOut> onSuccess, Func<TError, TOut> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

        public Task<TOut> MatchAsync<TOut>(Func<TResult, Task<TOut>> onSuccess, Func<TError, Task<TOut>> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

        public ValueTask<TOut> MatchAsync<TOut>(Func<TResult, ValueTask<TOut>> onSuccess, Func<TError, ValueTask<TOut>> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

        public ValueTask<TOut> MatchAsync<TOut>(Func<TResult, ValueTask<TOut>> onSuccess, Func<TError, TOut> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => onSuccess(ok.Value),
                Fail<TResult, TError> fail => ValueTask.FromResult(onFailure(fail.Error)),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

        public ValueTask<TOut> MatchAsync<TOut>(Func<TResult, TOut> onSuccess, Func<TError, ValueTask<TOut>> onFailure)
            => result switch
            {
                Ok<TResult, TError> ok => ValueTask.FromResult(onSuccess(ok.Value)),
                Fail<TResult, TError> fail => onFailure(fail.Error),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };
    }

    extension<TResult, TError>(Task<Result<TResult, TError>> resultTask)
    {
        public Task<Result<TResult, TError>> Tap(Action<TResult> onSuccess, Action<TError>? onFailure = null) => resultTask.ContinueWith(task => task.Result.Tap(onSuccess, onFailure), TaskContinuationOptions.ExecuteSynchronously);

        public async Task<Result<TResult, TError>> TapAsync(Action<TResult> onSuccess, Action<TError>? onFailure = null)
        {
            var result = await resultTask.ConfigureAwait(false);

            return result.Tap(onSuccess, onFailure);
        }

        public async Task<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, Task<Result<TMapped, TError>>> binder)
        {
            var result = await resultTask.ConfigureAwait(false);

            return await result.BindAsync(binder).ConfigureAwait(false);
        }

        public async Task<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, ValueTask<Result<TMapped, TError>>> binder)
        {
            var result = await resultTask.ConfigureAwait(false);

            return await result.BindAsync(binder).ConfigureAwait(false);
        }

        public async Task<Result<TResult, TError>> OrElseAsync(Func<TError, Task<Result<TResult, TError>>> fallback)
            => await resultTask.ConfigureAwait(false) switch
            {
                Ok<TResult, TError> ok => ok,
                Fail<TResult, TError> fail => await fallback(fail.Error).ConfigureAwait(false),
                _ => throw new InvalidOperationException(UnexpectedResultTypeMessage)
            };

        public async Task<Result<TResult, TError>> EnsureAsync(Action finallyAction)
        {
            var result = await resultTask.ConfigureAwait(false);
            finallyAction();

            return result;
        }

        public async Task<Result<TResult, TError>> EnsureAsync(Func<ValueTask> finallyAction)
        {
            var result = await resultTask.ConfigureAwait(false);
            await finallyAction().ConfigureAwait(false);

            return result;
        }

        public async Task<TOut> MatchAsync<TOut>(Func<TResult, Task<TOut>> onSuccess, Func<TError, Task<TOut>> onFailure)
            => await (await resultTask.ConfigureAwait(false)).MatchAsync(onSuccess, onFailure).ConfigureAwait(false);

        public async Task<TOut> MatchAsync<TOut>(Func<TResult, TOut> onSuccess, Func<TError, TOut> onFailure)
            => (await resultTask.ConfigureAwait(false)).Match(onSuccess, onFailure);
    }

    extension<TResult, TError>(ValueTask<Result<TResult, TError>> resultTask)
    {
        public async ValueTask<Result<TResult, TError>> TapAsync(Action<TResult> onSuccess, Action<TError>? onFailure = null)
        {
            var result = await resultTask.ConfigureAwait(false);

            return result.Tap(onSuccess, onFailure);
        }

        public async ValueTask<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, Task<Result<TMapped, TError>>> binder)
        {
            var result = await resultTask.ConfigureAwait(false);

            return await result.BindAsync(binder).ConfigureAwait(false);
        }

        public async ValueTask<Result<TMapped, TError>> BindAsync<TMapped>(Func<TResult, ValueTask<Result<TMapped, TError>>> binder)
        {
            var result = await resultTask.ConfigureAwait(false);

            return await result.BindAsync(binder).ConfigureAwait(false);
        }

        public async ValueTask<Result<TResult, TError>> EnsureAsync(Action finallyAction)
        {
            var result = await resultTask.ConfigureAwait(false);
            finallyAction();

            return result;
        }

        public async ValueTask<Result<TResult, TError>> EnsureAsync(Func<ValueTask> finallyAction)
        {
            var result = await resultTask.ConfigureAwait(false);
            await finallyAction().ConfigureAwait(false);

            return result;
        }
    }
}
