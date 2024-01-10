using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MipInEx;

/// <summary>
/// The manifest details for an assembly asset.
/// </summary>
public sealed class ModAssemblyManifest : IModAssetManifest
{
    private readonly string assetPath;
    private readonly long loadPriority;
    private readonly ModRootPluginManifest plugin;
    private readonly ImmutableArray<ModInternalPluginManifest> internalPlugins;

    /// <summary>
    /// Initializes this assembly settings with the specified
    /// name, priority, and the plugin settings.
    /// </summary>
    /// <param name="assetPath">
    /// The asset path of the assembly.
    /// </param>
    /// <param name="loadPriority">
    /// The load priority of this assembly. The higher the
    /// value, the higher the priority.
    /// </param>
    /// <param name="plugin">
    /// The settings for the root plugin in the assembly.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assetPath"/> is <see langword="null"/>.
    /// </exception>
    public ModAssemblyManifest(string assetPath, long loadPriority, ModRootPluginManifest? plugin)
    {
        this.assetPath = Utility.ValidateAssetPath(assetPath);
        this.loadPriority = loadPriority;
        this.plugin = plugin ?? new ModRootPluginManifest();
        this.internalPlugins = ImmutableArray<ModInternalPluginManifest>.Empty;
    }

    /// <summary>
    /// Initializes this assembly settings with the
    /// specified name, priority, the plugin settings, and a
    /// collection of internal plugin settings.
    /// </summary>
    /// <param name="assetPath">
    /// The asset path of the assembly.
    /// </param>
    /// <param name="loadPriority">
    /// The load priority of this assembly. The higher the
    /// value, the higher the priority.
    /// </param>
    /// <param name="plugin">
    /// The settings for the root plugin in the assembly.
    /// </param>
    /// <param name="internalPlugins">
    /// A collection of the settings for the internal plugins
    /// in the assembly.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assetPath"/> is <see langword="null"/>.
    /// </exception>
    public ModAssemblyManifest(string assetPath, long loadPriority, ModRootPluginManifest? plugin, IEnumerable<ModInternalPluginManifest?>? internalPlugins)
    {

        this.assetPath = Utility.ValidateAssetPath(assetPath);
        this.loadPriority = loadPriority;
        this.plugin = plugin ?? new ModRootPluginManifest();
        this.internalPlugins = internalPlugins is null ?
            ImmutableArray<ModInternalPluginManifest>.Empty :
            internalPlugins.OfType<ModInternalPluginManifest>().ToImmutableArray();
    }

    /// <summary>
    /// The asset path of this assembly.
    /// </summary>
    /// <remarks>
    /// The assembly will be located at
    /// <c>ModRootFolder/Assemblies/$(AssetPath).dll</c>
    /// where <c>$(AssetPath)</c> is this asset path value.
    /// </remarks>
    public string AssetPath => this.assetPath;

    /// <summary>
    /// The long asset path of this assembly.
    /// </summary>
    /// <remarks>
    /// Returns
    /// <c>"Assemblies/{<see langword="this"/>.<see cref="AssetPath">AssetPath</see>}.dll"</c>
    /// </remarks>
    public string FullAssetPath => "Assemblies/" + this.assetPath + ".dll";

    /// <summary>
    /// The settings for the root plugin in this assembly.
    /// </summary>
    public ModRootPluginManifest Plugin => this.plugin;

    /// <summary>
    /// The load priority of this assembly. The higher the
    /// value, the higher the priority.
    /// </summary>
    /// <remarks>
    /// Assemblies not specified will have a load priority of
    /// <c>0</c>.
    /// </remarks>
    public long LoadPriority => this.loadPriority;

    /// <summary>
    /// A collection of the settings for the internal plugins
    /// in the assembly.
    /// </summary>
    public IReadOnlyList<ModInternalPluginManifest> InternalPlugins => this.internalPlugins;

    /// <summary>
    /// The type of this asset.
    /// </summary>
    /// <remarks>
    /// Will always be <see cref="ModAssetType.Assembly"/>.
    /// </remarks>
    public ModAssetType Type => ModAssetType.Assembly;

    /// <summary>
    /// Creates a <see cref="ModAssemblyManifest"/> using the
    /// default settings.
    /// </summary>
    /// <param name="assetPath">
    /// The asset path of the assembly.
    /// </param>
    /// <returns>
    /// A <see cref="ModAssemblyManifest"/> with default
    /// settings.
    /// </returns>
    public static ModAssemblyManifest CreateDefault(string assetPath)
    {
        return new ModAssemblyManifest(assetPath, 0, null, null);
    }
}
