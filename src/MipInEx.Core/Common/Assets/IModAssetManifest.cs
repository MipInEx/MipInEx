namespace MipInEx;

/// <summary>
/// An asset in a manifest.
/// </summary>
public interface IModAssetManifest
{
    /// <summary>
    /// The short asset path of this asset.
    /// </summary>
    string AssetPath { get; }

    /// <summary>
    /// The long asset path of this asset.
    /// </summary>
    string FullAssetPath { get; }

    /// <summary>
    /// The load priority of this asset. The higher the value,
    /// the higher the priority.
    /// </summary>
    long LoadPriority { get; }

    /// <summary>
    /// The type of this asset.
    /// </summary>
    ModAssetType Type { get; }
}
