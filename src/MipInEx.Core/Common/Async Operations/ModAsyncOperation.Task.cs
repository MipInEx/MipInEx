
using System;
using System.Threading.Tasks;

namespace MipInEx;

partial class ModAsyncOperation
{
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
}