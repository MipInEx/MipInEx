using System;

namespace MipInEx;

partial class ModAsyncOperation
{
    /// <summary>
    /// Represents a completed async mod operation.
    /// </summary>
    public static readonly ModAsyncOperation Completed = new CompletedOperation();

    /// <summary>
    /// Creates a completed
    /// <see cref="ModAsyncOperation{TResult}"/> with the given
    /// result.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result.
    /// </typeparam>
    /// <param name="result">
    /// The result.
    /// </param>
    /// <returns>
    /// A completed <see cref="ModAsyncOperation{TResult}"/>
    /// with a result of <paramref name="result"/>.
    /// </returns>
    public static ModAsyncOperation<TResult> FromResult<TResult>(TResult result)
    {
        return new CompletedOperation<TResult>(result);
    }

    private sealed class CompletedOperation : ModAsyncOperation
    {
        public sealed override ModAsyncOperationStatus Status => ModAsyncOperationStatus.SuccessComplete;
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => true;
        public sealed override bool IsCompletedSuccessfully => true;
        public sealed override bool IsFaulted => false;
        public sealed override Exception? Exception => null;

        public sealed override double GetProgress()
        {
            return 1.0;
        }

        public sealed override bool Process()
        {
            return true;
        }

    }

    private sealed class CompletedOperation<TResult> : ModAsyncOperation<TResult>
    {
        private readonly TResult value;

        public CompletedOperation(TResult value)
        {
            this.value = value;
        }

        public sealed override ModAsyncOperationStatus Status => ModAsyncOperationStatus.SuccessComplete;
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => true;
        public sealed override bool IsCompletedSuccessfully => true;
        public sealed override bool IsFaulted => false;
        public sealed override Exception? Exception => null;
        public sealed override TResult? Result => this.value;

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
