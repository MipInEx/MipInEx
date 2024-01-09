using System;
using System.Threading.Tasks;

namespace MipInEx;

/// <summary>
/// The status of a mod async operation.
/// </summary>
public enum ModAsyncOperationStatus
{
    /// <summary>
    /// The async operation hasn't been started.
    /// </summary>
    NotStarted,
    /// <summary>
    /// The async operation is running.
    /// </summary>
    Running,
    /// <summary>
    /// The async operation completed successfully.
    /// </summary>
    SuccessComplete,
    /// <summary>
    /// The async operation completed with an error.
    /// </summary>
    FaultComplete
}

/// <summary>
/// An async operation in a mod.
/// </summary>
public abstract class ModAsyncOperation
{
    /// <summary>
    /// The current state of the async operation.
    /// </summary>
    public abstract ModAsyncOperationStatus Status { get; }

    /// <summary>
    /// Whether or not this async operation is running.
    /// </summary>
    public abstract bool IsRunning { get; }

    /// <summary>
    /// Whether or not this async operation is completed.
    /// </summary>
    public abstract bool IsCompleted { get; }

    /// <summary>
    /// Whether or not this async operation was completed
    /// successfully.
    /// </summary>
    public abstract bool IsCompletedSuccessfully { get; }

    /// <summary>
    /// Whether or not this async operation was completed
    /// due to an early exception.
    /// </summary>
    public abstract bool IsFaulted { get; }

    /// <summary>
    /// The exception of this async operation. Will only exist
    /// if this async operation was faulted.
    /// </summary>
    public abstract Exception? Exception { get; }

    /// <summary>
    /// Gets the progress of this async operation.
    /// </summary>
    /// <returns>
    /// <c>0</c> if not started, <c>1</c> if complete, or a
    /// value between <c>0</c> and <c>1</c> denoting how
    /// complete the operation is.
    /// </returns>
    public abstract double GetProgress();

    /// <summary>
    /// Gets a description string that is a description of the
    /// current work being done in this async operation.
    /// </summary>
    /// <returns>
    /// A description of the current work being done, or
    /// <see langword="null"/> if not implemented or not
    /// working.
    /// </returns>
    public virtual string? GetDescriptionString()
        => null;

    /// <summary>
    /// Processes the async operation, updating its state.
    /// </summary>
    /// <returns>
    /// Whether or not the async operation completed.
    /// </returns>
    public abstract bool Process();

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

    /// <summary>
    /// Creates a new <see cref="ModAsyncOperation"/> with the
    /// given task.
    /// </summary>
    /// <param name="task">
    /// The task.
    /// </param>
    /// <returns>
    /// A <see cref="ModAsyncOperation"/> that is a wrapper for
    /// <paramref name="task"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="task"/> is <see langword="null"/>.
    /// </exception>
    public static ModAsyncOperation FromTask(Task task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        return new TaskOperation(task);
    }

    /// <summary>
    /// Creates a new <see cref="ModAsyncOperation{TResult}"/>
    /// with the given task.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result produced by the task.
    /// </typeparam>
    /// <param name="task">
    /// The task.
    /// </param>
    /// <returns>
    /// A <see cref="ModAsyncOperation{TResult}"/> that is a
    /// wrapper for <paramref name="task"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="task"/> is <see langword="null"/>.
    /// </exception>
    public static ModAsyncOperation<TResult> FromTask<TResult>(Task<TResult> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        return new TaskOperation<TResult>(task);
    }

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

    private sealed class TaskOperation : ModAsyncOperation
    {
        private readonly Task task;
        private ModAsyncOperationStatus status;
        private Exception? exception;

        public TaskOperation(Task task)
        {
            this.task = task;
            this.Process();
        }

        public sealed override ModAsyncOperationStatus Status => this.status;
        public sealed override bool IsRunning => this.status is ModAsyncOperationStatus.Running;
        public sealed override bool IsCompleted => this.status is ModAsyncOperationStatus.SuccessComplete or ModAsyncOperationStatus.FaultComplete;
        public sealed override bool IsCompletedSuccessfully => this.status is ModAsyncOperationStatus.SuccessComplete;
        public sealed override bool IsFaulted => this.status is ModAsyncOperationStatus.FaultComplete;
        public sealed override Exception? Exception => this.exception;

        public sealed override double GetProgress()
        {
            return this.status switch
            {
                ModAsyncOperationStatus.NotStarted => 0.0,
                ModAsyncOperationStatus.Running => 0.5,
                ModAsyncOperationStatus.SuccessComplete or ModAsyncOperationStatus.FaultComplete => 1.0,
                _ => 0.0
            };
        }

        public sealed override bool Process()
        {
            if (this.status is ModAsyncOperationStatus.SuccessComplete or ModAsyncOperationStatus.FaultComplete)
            {
                return true;
            }

            TaskStatus taskStatus = this.task.Status;
            if (taskStatus >= TaskStatus.Created && taskStatus <= TaskStatus.WaitingToRun)
            {
                this.status = ModAsyncOperationStatus.NotStarted;
            }
            else if (taskStatus is TaskStatus.Running or TaskStatus.WaitingForChildrenToComplete)
            {
                this.status = ModAsyncOperationStatus.Running;
            }
            else if (taskStatus is TaskStatus.RanToCompletion)
            {
                this.status = ModAsyncOperationStatus.SuccessComplete;
            }
            else if (taskStatus is TaskStatus.Faulted)
            {
                this.status = ModAsyncOperationStatus.FaultComplete;
                this.exception = this.task.Exception;
            }
            else if (taskStatus is TaskStatus.Canceled)
            {
                this.status = ModAsyncOperationStatus.FaultComplete;
                this.exception = new TaskCanceledException(this.task);
            }

            return this.status is ModAsyncOperationStatus.SuccessComplete or ModAsyncOperationStatus.FaultComplete;
        }
    }

    private sealed class TaskOperation<TResult> : ModAsyncOperation<TResult>
    {
        private readonly Task<TResult> task;
        private ModAsyncOperationStatus status;
        private Exception? exception;
        private TResult? result;

        public TaskOperation(Task<TResult> task)
        {
            this.task = task;
            this.Process();
        }

        public sealed override ModAsyncOperationStatus Status => this.status;
        public sealed override bool IsRunning => this.status is ModAsyncOperationStatus.Running;
        public sealed override bool IsCompleted => this.status is ModAsyncOperationStatus.SuccessComplete or ModAsyncOperationStatus.FaultComplete;
        public sealed override bool IsCompletedSuccessfully => this.status is ModAsyncOperationStatus.SuccessComplete;
        public sealed override bool IsFaulted => this.status is ModAsyncOperationStatus.FaultComplete;
        public sealed override Exception? Exception => this.exception;
        public sealed override TResult? Result => this.result;

        public sealed override double GetProgress()
        {
            return this.status switch
            {
                ModAsyncOperationStatus.NotStarted => 0.0,
                ModAsyncOperationStatus.Running => 0.5,
                ModAsyncOperationStatus.SuccessComplete or ModAsyncOperationStatus.FaultComplete => 1.0,
                _ => 0.0
            };
        }

        public sealed override bool Process()
        {
            if (this.status is ModAsyncOperationStatus.SuccessComplete or ModAsyncOperationStatus.FaultComplete)
            {
                return true;
            }

            TaskStatus taskStatus = this.task.Status;
            if (taskStatus >= TaskStatus.Created && taskStatus <= TaskStatus.WaitingToRun)
            {
                this.status = ModAsyncOperationStatus.NotStarted;
            }
            else if (taskStatus is TaskStatus.Running or TaskStatus.WaitingForChildrenToComplete)
            {
                this.status = ModAsyncOperationStatus.Running;
            }
            else if (taskStatus is TaskStatus.RanToCompletion)
            {
                this.status = ModAsyncOperationStatus.SuccessComplete;
                this.result = this.task.Result;
            }
            else if (taskStatus is TaskStatus.Faulted)
            {
                this.status = ModAsyncOperationStatus.FaultComplete;
                this.exception = this.task.Exception;
            }
            else if (taskStatus is TaskStatus.Canceled)
            {
                this.status = ModAsyncOperationStatus.FaultComplete;
                this.exception = new TaskCanceledException(this.task);
            }

            return this.status is ModAsyncOperationStatus.SuccessComplete or ModAsyncOperationStatus.FaultComplete;
        }
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

/// <summary>
/// Represents an async operation in a mod that can have a
/// result.
/// </summary>
public abstract class ModAsyncOperationWithResult : ModAsyncOperation
{
    /// <summary>
    /// Gets the result of this async operation.
    /// </summary>
    /// <returns>
    /// The result of this async operation.
    /// </returns>
    public abstract object? GetResult();
}

/// <inheritdoc cref="ModAsyncOperation"/>
/// <typeparam name="TResult">
/// The type of result.
/// </typeparam>
public abstract class ModAsyncOperation<TResult> : ModAsyncOperationWithResult
{
    /// <summary>
    /// The result of the operation. Set when this operation
    /// is completed.
    /// </summary>
    public abstract TResult? Result { get; }

    /// <inheritdoc/>
    public sealed override object? GetResult()
    {
        return this.Result;
    }
}

/// <summary>
/// An async operation that performs multiple inner async
/// operations.
/// </summary>
public abstract class ModAsyncMultiOperation : ModAsyncOperation
{
    /// <summary>
    /// Gets the total number of operations this async
    /// operation will perform.
    /// </summary>
    public abstract int OperationCount { get; }

    /// <summary>
    /// Gets the total number of completed operations
    /// this async operation performed.
    /// </summary>
    public abstract int CompletedOperationCount { get; }

    /// <summary>
    /// Gets the combined progress of all operations in this
    /// async operation.
    /// </summary>
    /// <returns>
    /// The combined progress of all operations in this async
    /// operation.
    /// </returns>
    public abstract double GetTotalProgress();
}