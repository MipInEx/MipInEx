namespace MipInEx;

/// <summary>
/// The info about a mod's asset.
/// </summary>
public interface IModAssetInfo
{
    /// <inheritdoc cref="IModAsset.Name"/>
    string Name { get; }

    /// <inheritdoc cref="IModAsset.AssetPath"/>
    string AssetPath { get; }

    /// <inheritdoc cref="IModAsset.FullAssetPath"/>
    string FullAssetPath { get; }

    /// <inheritdoc cref="IModAsset.Mod"/>
    ModInfo Mod { get; }

    /// <inheritdoc cref="IModAsset.State"/>
    ModAssetState State { get; }

    /// <inheritdoc cref="IModAsset.Type"/>
    ModAssetType Type { get; }

    /// <inheritdoc cref="IModAsset.GetDescriptorString()"/>
    string GetDescriptorString();
}
