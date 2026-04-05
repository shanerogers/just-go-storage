using LanguageExt;
using static LanguageExt.Prelude;

namespace JustGo.Api.Common;

/// <summary>
/// Extension methods for LanguageExt Either types.
/// </summary>
internal static class EitherExtensions
{
    public static async Task<Either<TLeft, TNext>> BindAsync<TLeft, TRight, TNext>(
        this Task<Either<TLeft, TRight>> eitherTask,
        Func<TRight, Task<Either<TLeft, TNext>>> bind)
    {
        var either = await eitherTask;

        return await either.Match(
            Right: bind,
            Left: left => Task.FromResult((Either<TLeft, TNext>)left));
    }

    /// <summary>
    /// Executes a side effect on the Right (success) case and returns the Either unchanged,
    /// allowing for fluent chaining of multiple side effects before a terminal operation.
    /// </summary>
    /// <typeparam name="TLeft">The Left (error) type.</typeparam>
    /// <typeparam name="TRight">The Right (success) type.</typeparam>
    /// <param name="either">The Either to tap into.</param>
    /// <param name="action">The side effect to execute if Right.</param>
    /// <returns>The original Either unchanged.</returns>
    public static Either<TLeft, TRight> Tap<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        Action<TRight> action)
    {
        either.IfRight(action);
        return either;
    }

    public static async Task<Either<TLeft, TRight>> TapAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> eitherTask,
        Action<TRight> action)
    {
        var either = await eitherTask;
        either.IfRight(action);
        return either;
    }

    /// <summary>
    /// Executes a side effect on an EitherAsync in the Right (success) case and returns the EitherAsync unchanged,
    /// allowing for fluent chaining of multiple side effects before a terminal operation.
    /// </summary>
    /// <typeparam name="TLeft">The Left (error) type.</typeparam>
    /// <typeparam name="TRight">The Right (success) type.</typeparam>
    /// <param name="eitherAsync">The EitherAsync to tap into.</param>
    /// <param name="action">The side effect to execute if Right.</param>
    /// <returns>The original EitherAsync unchanged.</returns>
    public static EitherAsync<TLeft, TRight> Tap<TLeft, TRight>(
        this EitherAsync<TLeft, TRight> eitherAsync,
        Action<TRight> action)
    {
        return eitherAsync.Map(right =>
        {
            action(right);
            return right;
        });
    }

    /// <summary>
    /// Executes an async side effect on an EitherAsync in the Right (success) case and returns the EitherAsync unchanged,
    /// allowing for fluent chaining of multiple async side effects before a terminal operation.
    /// </summary>
    /// <typeparam name="TLeft">The Left (error) type.</typeparam>
    /// <typeparam name="TRight">The Right (success) type.</typeparam>
    /// <param name="eitherAsync">The EitherAsync to tap into.</param>
    /// <param name="asyncAction">The async side effect to execute if Right.</param>
    /// <returns>The original EitherAsync unchanged.</returns>
    public static async Task<Either<TLeft, TRight>> TapAsync<TLeft, TRight>(
        this EitherAsync<TLeft, TRight> eitherAsync,
        Func<TRight, Task> asyncAction)
    {
        var either = await eitherAsync;
        return await either.Match(
            async right =>
            {
                await asyncAction(right);
                return Right<TLeft, TRight>(right);
            },
            left => Task.FromResult(Left<TLeft, TRight>(left)));
    }

    /// <summary>
    /// Executes a side effect on the Left (error) case and returns the Either unchanged,
    /// allowing for fluent chaining of error-handling side effects before a terminal operation.
    /// </summary>
    /// <typeparam name="TLeft">The Left (error) type.</typeparam>
    /// <typeparam name="TRight">The Right (success) type.</typeparam>
    /// <param name="either">The Either to tap into.</param>
    /// <param name="action">The side effect to execute if Left.</param>
    /// <returns>The original Either unchanged.</returns>
    public static Either<TLeft, TRight> TapLeft<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        Action<TLeft> action)
    {
        either.IfLeft(action);
        return either;
    }

    /// <summary>
    /// Executes a side effect on an EitherAsync in the Left (error) case and returns the EitherAsync unchanged,
    /// allowing for fluent chaining of error-handling side effects before a terminal operation.
    /// </summary>
    /// <typeparam name="TLeft">The Left (error) type.</typeparam>
    /// <typeparam name="TRight">The Right (success) type.</typeparam>
    /// <param name="eitherAsync">The EitherAsync to tap into.</param>
    /// <param name="action">The side effect to execute if Left.</param>
    /// <returns>The original EitherAsync unchanged.</returns>
    public static EitherAsync<TLeft, TRight> TapLeft<TLeft, TRight>(
        this EitherAsync<TLeft, TRight> eitherAsync,
        Action<TLeft> action)
    {
        return eitherAsync.MapLeft(left =>
        {
            action(left);
            return left;
        });
    }

    /// <summary>
    /// Executes an async side effect on an EitherAsync in the Left (error) case and returns the EitherAsync unchanged,
    /// allowing for fluent chaining of async error-handling side effects before a terminal operation.
    /// </summary>
    /// <typeparam name="TLeft">The Left (error) type.</typeparam>
    /// <typeparam name="TRight">The Right (success) type.</typeparam>
    /// <param name="eitherAsync">The EitherAsync to tap into.</param>
    /// <param name="asyncAction">The async side effect to execute if Left.</param>
    /// <returns>The original EitherAsync unchanged.</returns>
    public static async Task<Either<TLeft, TRight>> TapLeftAsync<TLeft, TRight>(
        this EitherAsync<TLeft, TRight> eitherAsync,
        Func<TLeft, Task> asyncAction)
    {
        var either = await eitherAsync;
        return await either.Match(
            right => Task.FromResult(Right<TLeft, TRight>(right)),
            async left =>
            {
                await asyncAction(left);
                return Left<TLeft, TRight>(left);
            });
    }
}
