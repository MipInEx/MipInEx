using System;

namespace MipInEx;

partial class ModAsyncOperation
{
    /// <summary>
    /// Creates a new <see cref="ModAsyncOperation"/> with the
    /// given sync operation.
    /// </summary>
    /// <param name="operation">
    /// The sync operation.
    /// </param>
    /// <returns>
    /// A <see cref="ModAsyncOperation"/> that runs the sync
    /// operation <paramref name="operation"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="operation"/> is <see langword="null"/>.
    /// </exception>
    public static ModAsyncOperation FromOperation(Action operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        return new SyncOperation(operation);
    }

    /// <summary>
    /// Creates a new <see cref="ModAsyncOperation{TResult}"/>
    /// with the given sync operation.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result of the sync operation.
    /// </typeparam>
    /// <param name="operation">
    /// The sync operation.
    /// </param>
    /// <returns>
    /// A <see cref="ModAsyncOperation{TResult}"/> that runs
    /// the sync operation <paramref name="operation"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="operation"/> is <see langword="null"/>.
    /// </exception>
    public static ModAsyncOperation<TResult> FromOperation<TResult>(Func<TResult> operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        return new SyncOperation<TResult>(operation);
    }

    private sealed class SyncOperation : ModAsyncOperation
    {
        private readonly Action operation;
        private Exception? exception;
        private bool isDone;

        public SyncOperation(Action operation)
        {
            this.operation = operation;
            this.exception = null;
            this.isDone = false;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone)
                {
                    if (this.exception is null) return ModAsyncOperationStatus.SuccessComplete;
                    else return ModAsyncOperationStatus.FaultComplete;
                }
                else return ModAsyncOperationStatus.NotStarted;
            }
        }

        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone && this.exception is null;
        public sealed override bool IsFaulted => this.isDone && this.exception is not null;
        public sealed override Exception? Exception => this.exception;

        public sealed override double GetProgress()
        {
            return this.isDone ? 1.0 : 0.0;
        }

        public sealed override bool Process()
        {
            if (this.isDone) return true;
            try
            {
                this.operation.Invoke();
            }
            catch (Exception ex)
            {
                this.exception = ex;
            }
            this.isDone = true;
            return true;
        }
    }

    private sealed class SyncOperation<TResult> : ModAsyncOperation<TResult>
    {
        private readonly Func<TResult> operation;
        private Exception? exception;
        private TResult? result;
        private bool isDone;

        public SyncOperation(Func<TResult> operation)
        {
            this.operation = operation;
            this.exception = null;
            this.result = default;
            this.isDone = false;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone)
                {
                    if (this.exception is null) return ModAsyncOperationStatus.SuccessComplete;
                    else return ModAsyncOperationStatus.FaultComplete;
                }
                else return ModAsyncOperationStatus.NotStarted;
            }
        }
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone && this.exception is null;
        public sealed override bool IsFaulted => this.isDone && this.exception is not null;
        public sealed override Exception? Exception => this.exception;
        public sealed override TResult? Result => this.result;

        public sealed override double GetProgress()
        {
            return this.isDone ? 1.0 : 0.0;
        }

        public sealed override bool Process()
        {
            if (this.isDone) return true;
            try
            {
                this.result = this.operation.Invoke();
            }
            catch (Exception ex)
            {
                this.exception = ex;
            }
            this.isDone = true;
            return true;
        }
    }
}
