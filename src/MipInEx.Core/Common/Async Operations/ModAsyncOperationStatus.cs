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
