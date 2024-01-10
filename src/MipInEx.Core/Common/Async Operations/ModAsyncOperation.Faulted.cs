using System;

namespace MipInEx;

partial class ModAsyncOperation
{
    /// <summary>
    /// Creates a faulted <see cref="ModAsyncOperation"/> with
    /// the given exception.
    /// </summary>
    /// <param name="exception">
    /// The exception.
    /// </param>
    /// <returns>
    /// A faulted <see cref="ModAsyncOperation"/> with an
    /// exception of <paramref name="exception"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public static ModAsyncOperation FromException(Exception exception)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));
        return new FaultedOperation(exception);
    }

    /// <summary>
    /// Creates a faulted
    /// <see cref="ModAsyncOperation{TResult}"/> with the given
    /// exception.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of result.
    /// </typeparam>
    /// <param name="exception">
    /// The exception.
    /// </param>
    /// <returns>
    /// A faulted <see cref="ModAsyncOperation{TResult}"/> with
    /// an exception of <paramref name="exception"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public static ModAsyncOperation<TResult> FromException<TResult>(Exception exception)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));
        return new FaultedOperation<TResult>(exception);
    }

    private sealed class FaultedOperation : ModAsyncOperation
    {
        private readonly Exception exception;

        public FaultedOperation(Exception exception)
        {
            this.exception = exception;
        }

        public sealed override ModAsyncOperationStatus Status => ModAsyncOperationStatus.FaultComplete;
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => true;
        public sealed override bool IsCompletedSuccessfully => false;
        public sealed override bool IsFaulted => true;
        public sealed override Exception? Exception => this.exception;

        public sealed override double GetProgress()
        {
            return 1.0;
        }

        public sealed override bool Process()
        {
            return true;
        }
    }

    private sealed class FaultedOperation<TResult> : ModAsyncOperation<TResult>
    {
        private readonly Exception exception;

        public FaultedOperation(Exception exception)
        {
            this.exception = exception;
        }

        public sealed override ModAsyncOperationStatus Status => ModAsyncOperationStatus.FaultComplete;
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => true;
        public sealed override bool IsCompletedSuccessfully => false;
        public sealed override bool IsFaulted => true;
        public sealed override Exception? Exception => this.exception;
        public sealed override TResult? Result => default;

        public sealed override double GetProgress()
        {
            return 1.0;
        }

        public sealed override bool Process()
        {
            return true;
        }
    }
}
