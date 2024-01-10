using System;

namespace MipInEx;

/// <summary>
/// The manifest details for an asset bundle.
/// </summary>
public sealed class ModAssetBundleManifest : IModAssetManifest
{
    private readonly string assetPath;
    private readonly long loadPriority;
    private readonly bool loadManually;

    /// <summary>
    /// Initializes this asset bundle settings with the
    /// specified name, priority, and whether or not the asset
    /// bundle needs to be loaded manually.
    /// </summary>
    /// <param name="assetPath">
    /// The asset path of the asset bundle.
    /// </param>
    /// <param name="loadPriority">
    /// The load priority of this asset bundle. The higher the
    /// value, the higher the priority.
    /// </param>
    /// <param name="loadManually">
    /// Whether or not this asset bundle needs to be manually
    /// loaded.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assetPath"/> is <see langword="null"/>.
    /// </exception>
    public ModAssetBundleManifest(string assetPath, long loadPriority, bool loadManually)
    {
        this.assetPath = Utility.ValidateAssetPath(assetPath);
        this.loadPriority = loadPriority;
        this.loadManually = loadManually;
    }

    /// <summary>
    /// The asset path of this asset bundle.
    /// </summary>
    /// <remarks>
    /// The asset bundle will be located at
    /// <c>ModRootFolder/Asset Bundles/$(AssetPath).bundle</c>
    /// where <c>$(AssetPath)</c> is this asset path value.
    /// </remarks>
    public string AssetPath => this.assetPath;

    /// <summary>
    /// The long asset path of this asset bundle.
    /// </summary>
    /// <remarks>
    /// Returns
    /// <c>"Asset Bundles/{<see langword="this"/>.<see cref="AssetPath">AssetPath</see>}.bundle"</c>
    /// </remarks>
    public string FullAssetPath => "Asset Bundles/" + this.assetPath + ".bundle";

    /// <summary>
    /// The load priority of this asset bundle. The higher the
    /// value, the higher the priority.
    /// </summary>
    /// <remarks>
    /// Asset bundles not specified will have a load priority
    /// of <c>0</c>.
    /// </remarks>
    public long LoadPriority => this.loadPriority;

    /// <summary>
    /// Whether or not this asset bundle needs to be explicitly
    /// loaded (aka manually loaded)
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>.
    /// </remarks>
    public bool LoadManually => this.loadManually;

    /// <summary>
    /// The type of this asset.
    /// </summary>
    /// <remarks>
    /// Will always be <see cref="ModAssetType.AssetBundle"/>.
    /// </remarks>
    public ModAssetType Type => ModAssetType.AssetBundle;

    /// <summary>
    /// Creates a <see cref="ModAssetBundleManifest"/> using
    /// the default settings.
    /// </summary>
    /// <param name="assetPath">
    /// The asset path of the asset bundle.
    /// </param>
    /// <returns>
    /// A <see cref="ModAssetBundleManifest"/> with default
    /// settings.
    /// </returns>
    public static ModAssetBundleManifest CreateDefault(string assetPath)
    {
        return new ModAssetBundleManifest(assetPath, 0, false);
    }
}
