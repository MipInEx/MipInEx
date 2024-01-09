namespace MipInEx;

/// <summary>
/// The state of a Mod Asset.
/// </summary>
public enum ModAssetState : byte
{
    /// <summary>
    /// The mod asset is not loaded. Is the initial state of
    /// the asset.
    /// </summary>
    NotLoaded = 0,

    /// <summary>
    /// The mod asset is being loaded.
    /// </summary>
    Loading = 1,

    /// <summary>
    /// The mod asset has loaded.
    /// </summary>
    Loaded = 2,

    /// <summary>
    /// The mod asset is being unloaded.
    /// </summary>
    Unloading = 3,

    /// <summary>
    /// The mod asset has been unloaded.
    /// </summary>
    Unloaded = 4
}
