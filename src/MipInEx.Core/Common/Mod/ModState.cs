namespace MipInEx;

/// <summary>
/// The state of a mod.
/// </summary>
public enum ModState : byte
{
    /// <summary>
    /// The mod has not been initialized yet.
    /// </summary>
    Uninitialized = 0,

    /// <summary>
    /// The mod is being loaded.
    /// </summary>
    Loading = 1,

    /// <summary>
    /// The mod is loaded.
    /// </summary>
    Loaded = 2,

    /// <summary>
    /// The mod is being unloaded.
    /// </summary>
    Unloading = 3,

    /// <summary>
    /// The mod has been unloaded.
    /// </summary>
    Unloaded = 4,

    /// <summary>
    /// The mod is errored.
    /// </summary>
    Error = 6
}
