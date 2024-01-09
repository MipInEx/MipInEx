using Mono.Cecil;
using System.Collections.Generic;

namespace MipInEx.Bootstrap;

/// <summary>
/// The context for mod initialization.
/// </summary>
internal sealed class ModInitializationContext
{
    public readonly string assetBundleFolderName;
    public readonly string assemblyFolderName;
    public readonly ReaderParameters readerParameters;
    public readonly Dictionary<string, CachedAssembly>? assemblyCache;
    public readonly List<Mod> mods;

    public ModInitializationContext(
        string assemblyFolderName,
        string assetBundleFolderName,
        Dictionary<string, CachedAssembly>? assemblyCache,
        ReaderParameters readerParameters)
    {
        this.assetBundleFolderName = assetBundleFolderName;
        this.readerParameters = readerParameters;
        this.assemblyCache = assemblyCache;
        this.assemblyFolderName = assemblyFolderName;
        this.mods = new();
    }
}