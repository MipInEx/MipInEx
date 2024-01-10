using System;

namespace MipInEx;

/// <summary>
/// An async operation in a mod.
/// </summary>
public abstract partial class ModAsyncOperation
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