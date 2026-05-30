using LanguageExt;
using static LanguageExt.Prelude;

namespace JustGo.Api.Common;

/// <summary>
/// Extension methods for LanguageExt Either types.
/// </summary>
internal static class EitherExtensions
{
    /// <typeparam name="TLeft">The Left (error) type.</typeparam>
    /// <typeparam name="TRight">The Right (success) type.</typeparam>
    /// <param name="either">The Either to tap into.</param>
    extension<TLeft, TRight>(Either<TLeft, TRight> either)
    {
        /// <summary>
        /// Executes a side effect on the Right (success) case and returns the Either unchanged,
        /// allowing for fluent chaining of multiple side effects before a terminal operation.
        /// </summary>
        /// <param name="action">The side effect to execute if Right.</param>
        /// <returns>The original Either unchanged.</returns>
        public Either<TLeft, TRight> Tap(
            Action<TRight> action)
        {
            either.IfRight(action);
            return either;
        }

        /// <summary>
        /// Executes a side effect on the Left (error) case and returns the Either unchanged,
        /// allowing for fluent chaining of error-handling side effects before a terminal operation.
        /// </summary>
        /// <param name="action">The side effect to execute if Left.</param>
        /// <returns>The original Either unchanged.</returns>
        public Either<TLeft, TRight> TapLeft(
            Action<TLeft> action)
        {
            either.IfLeft(action);
            return either;
        }
    }

    /// <typeparam name="TLeft">The Left (error) type.</typeparam>
    /// <typeparam name="TRight">The Right (success) type.</typeparam>
    /// <param name="eitherAsync">The EitherAsync to tap into.</param>
    extension<TLeft, TRight>(EitherAsync<TLeft, TRight> eitherAsync)
    {
        /// <summary>
        /// Executes a side effect on an EitherAsync in the Right (success) case and returns the EitherAsync unchanged,
        /// allowing for fluent chaining of multiple side effects before a terminal operation.
        /// </summary>
        /// <param name="action">The side effect to execute if Right.</param>
        /// <returns>The original EitherAsync unchanged.</returns>
        public EitherAsync<TLeft, TRight> Tap(
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
        /// <param name="asyncAction">The async side effect to execute if Right.</param>
        /// <returns>The original EitherAsync unchanged.</returns>
        public async Task<Either<TLeft, TRight>> TapAsync(
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
        /// Executes a side effect on an EitherAsync in the Left (error) case and returns the EitherAsync unchanged,
        /// allowing for fluent chaining of error-handling side effects before a terminal operation.
        /// </summary>
        /// <param name="action">The side effect to execute if Left.</param>
        /// <returns>The original EitherAsync unchanged.</returns>
        public EitherAsync<TLeft, TRight> TapLeft(
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
        /// <param name="asyncAction">The async side effect to execute if Left.</param>
        /// <returns>The original EitherAsync unchanged.</returns>
        public async Task<Either<TLeft, TRight>> TapLeftAsync(
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
}
