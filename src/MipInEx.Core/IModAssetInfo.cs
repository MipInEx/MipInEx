namespace MipInEx;

/// <summary>
/// The info about a mod's asset.
/// </summary>
public interface IModAssetInfo
{
    /// <inheritdoc cref="IModAsset.Mod"/>
    ModInfo Mod { get; }

    /// <inheritdoc cref="IModAsset.State"/>
    ModAssetState State { get; }

    /// <inheritdoc cref="IModAsset.Type"/>
    ModAssetType Type { get; }

    /// <inheritdoc cref="IModAsset.GetDescriptorString()"/>
    string GetDescriptorString();
}
