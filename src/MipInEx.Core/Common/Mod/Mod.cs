using MipInEx.Bootstrap;
using MipInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace MipInEx;

// assume we have 4 mods that are like this:
// ModA
// - dependencies:
//   - ModB
//
// ModB
// - incompatibilities:
//   - ModC
//
// ModC
// - incompatibilities: 
//   - ModD
//
// ModD
//
// Assume we will auto-load:
// - ModA
// - ModB
// - ModC
//
// The following sort order should be achieved:
// - ModB
// - ModA
// - ModC
//
// 1. Loading ModB should fail, as ModC will also be loaded and
//    will result in an incompatibility.
// 2. Loading ModA should fail, as ModB wasn't loaded.
// 3. ModC should be loaded.
//
// Now assume we want to load ModD
// 1. We check all incompatibilities, and notice that ModC is
//    incompatible with ModD
// 2. We prompt the user:
//      Do you want to load ModD? Doing so will unload ModC!
// 3. If the user selects yes,
//   a. ModC should be unloaded
//   b. ModD should be loaded.
// 4. Otherwise if the user selects no,
//   Nothing should happen.

/// <summary>
/// The representation of a mod.
/// </summary>
public sealed class Mod
{
    private readonly ModManagerBase modManager;
    private readonly ModManifest manifest;
    private readonly string? readme;
    private readonly string? changelog;
    private readonly byte[]? iconData;

    private readonly ModInfo info;

    private readonly AssetCollection assets;
    private readonly AssetCollection<ModAssetBundle> assetBundles;
    private readonly AssetCollection<ModAssembly> assemblies;

    private ImmutableArray<Mod> incompatibilities;
    private ImmutableArray<ModDependencyInfo> missingDependencies;
    private ImmutableArray<Mod> requiredDependencies;
    private ImmutableArray<Mod> dependencies;

    private bool isEnabled;
    private bool isCircularDependency;


    internal Mod(
        ModManagerBase modManager,
        ModManifest manifest,
        string? readme,
        string? changelog,
        byte[]? iconData,
        ImmutableArray<ModImporter.AssetBundleInfo> assetBundles,
        ImmutableArray<ModImporter.AssemblyInfo> assemblies)
    {
        this.readme = readme;
        this.changelog = changelog;
        this.iconData = iconData;
        this.manifest = manifest;
        this.modManager = modManager;

        this.assetBundles = new AssetCollection<ModAssetBundle>(assetBundles
            .Select(x => new ModAssetBundle(
                this,
                x.manifest,
                x.importer))
            .ToImmutableArray());
        this.assemblies = new AssetCollection<ModAssembly>(assemblies
            .Select(x => new ModAssembly(
                this,
                x.manifest,
                x.importer,
                x.rootPluginReference,
                x.internalPluginReferences))
            .ToImmutableArray());
        this.assets = new AssetCollection(this.assetBundles.OfType<IModAsset>()
            .Concat(this.assemblies)
            .OrderBy(x => x, Utility.AssetPriorityComparer)
            .ToImmutableArray());

        this.incompatibilities = ImmutableArray<Mod>.Empty;

        this.missingDependencies = ImmutableArray<ModDependencyInfo>.Empty;
        this.requiredDependencies = ImmutableArray<Mod>.Empty;
        this.dependencies = ImmutableArray<Mod>.Empty;

        this.isCircularDependency = false;
        this.info = new(this);
    }

    /// <summary>
    /// The mod manager that manages this mod.
    /// </summary>
    public ModManagerBase ModManager => this.modManager;

    /// <summary>
    /// All assets in this mod.
    /// </summary>
    public AssetCollection Assets => this.assets;

    /// <summary>
    /// All assemblies in this mod.
    /// </summary>
    public AssetCollection<ModAssembly> Assemblies => this.assemblies;

    /// <summary>
    /// All asset bundles in this mod.
    /// </summary>
    public AssetCollection<ModAssetBundle> AssetBundles => this.assetBundles;

    /// <summary>
    /// All mods that this mod is incompatible with.
    /// </summary>
    public IReadOnlyList<Mod> Incompatibilities => this.incompatibilities;

    /// <summary>
    /// The required dependencies this mod is missing.
    /// </summary>
    public IReadOnlyList<ModDependencyInfo> MissingDependencies => this.missingDependencies;

    /// <summary>
    /// All mods required by this mod.
    /// </summary>
    public IReadOnlyList<Mod> RequiredDependencies => this.requiredDependencies;

    /// <summary>
    /// All mods this mod depends on (can be a soft or hard
    /// dependency).
    /// </summary>
    public IReadOnlyList<Mod> Dependencies => this.dependencies;

    internal bool IsCircularDependency => this.isCircularDependency;

    /// <inheritdoc cref="ModManifest.Name"/>
    public string Name => this.manifest.Name;
    /// <inheritdoc cref="ModManifest.Guid"/>
    public string Guid => this.manifest.Guid;
    /// <inheritdoc cref="ModManifest.Version"/>
    public Version Version => this.manifest.Version;
    /// <inheritdoc cref="ModManifest.Author"/>
    public string Author => this.manifest.Author;

    /// <summary>
    /// The manifest of this mod.
    /// </summary>
    public ModManifest Manifest => this.manifest;

    public bool IsEnabled => this.isEnabled;

    public ModInfo Info => this.info;

    public bool IsLoaded => this.info.IsLoaded;

    /// <inheritdoc cref="ModManifest.ToString()"/>
    public sealed override string ToString()
    {
        return this.manifest.ToString();
    }

    internal void RefreshIncompatibilitiesAndDependencies(ICollection<string> processedModGuids)
    {
        this.RefreshIncompatibilitiesAndDependencies(processedModGuids, new List<string>());
    }

    private void RefreshIncompatibilitiesAndDependencies(ICollection<string> processedModGuids, List<string> dependencyStack)
    {
        int existingDependencyIndex = dependencyStack.IndexOf(this.Guid);

        if (existingDependencyIndex > -1)
        {
            this.isCircularDependency = true;

            // this pass is here to mark the other dependencies
            // that requires this dependency as a circular
            // dependency too if it exists.
            //
            // This fixes the following bug.
            //
            // ModD
            //   Depends on ModE
            // ModE
            //   Depends on ModD
            //
            // In the load order:
            // - ModD
            // - ModE
            //
            // ModD would be skipped due to being marked a
            // circular dependency, but ModE wouldn't be marked
            // as a circular dependency and would be invalidly
            // loaded.

            for (int index = dependencyStack.Count - 1; index > existingDependencyIndex; index--)
            {
                if (this.modManager.FullRegistry.TryGetMod(dependencyStack[index], out Mod? mod))
                {
                    mod.isCircularDependency = true;
                }
            }
            return;
        }

        if (processedModGuids.Contains(this.Guid))
            return;

        ImmutableArray<Mod>.Builder incompatibilities = ImmutableArray.CreateBuilder<Mod>();

        ImmutableArray<Mod>.Builder requiredDependencies = ImmutableArray.CreateBuilder<Mod>();
        ImmutableArray<Mod>.Builder dependencies = ImmutableArray.CreateBuilder<Mod>();
        ImmutableArray<ModDependencyInfo>.Builder missingDependencies = ImmutableArray.CreateBuilder<ModDependencyInfo>();

        foreach (ModIncompatibilityInfo incompatibility in this.manifest.Incompatibilities)
        {
            if (this.modManager.FullRegistry.TryGetMod(incompatibility.Guid, out Mod? mod) &&
                incompatibility.IncludesVersion(mod.Version))
            {
                incompatibilities.Add(mod);
            }
        }

        dependencyStack.Add(this.Guid);
        foreach (ModDependencyInfo dependency in this.manifest.Dependencies)
        {
            if (this.modManager.FullRegistry.TryGetMod(dependency.Guid, out Mod? mod) &&
                dependency.IncludesVersion(mod.Version))
            {
                mod.RefreshIncompatibilitiesAndDependencies(processedModGuids, dependencyStack);
                dependencies.Add(mod);

                if (dependency.Required)
                    requiredDependencies.Add(mod);

                continue;
            }

            if (!dependency.Required)
                continue;

            missingDependencies.Add(dependency);
        }
        dependencyStack.RemoveAt(dependencyStack.Count - 1);
        processedModGuids.Add(this.Guid);

        this.incompatibilities = incompatibilities.ToImmutable();
        this.missingDependencies = missingDependencies.ToImmutable();
        this.requiredDependencies = requiredDependencies.ToImmutable();
        this.dependencies = dependencies.ToImmutable();

        this.info.RefreshIncompatibilitiesAndDependencies(
            this.incompatibilities,
            this.missingDependencies,
            this.requiredDependencies,
            this.dependencies);
    }

    public sealed class AssetCollection :
        IReadOnlyCollection<IModAsset>,
        ICollection<IModAsset>,
        ICollection,
        IEnumerable<IModAsset>,
        IEnumerable
    {
        private readonly FrozenDictionary<string, IModAsset> assetsByAssetPath;
        private readonly ImmutableArray<IModAsset> assets;

        internal AssetCollection(ImmutableArray<IModAsset> assets)
        {
            this.assets = assets;
            this.assetsByAssetPath = assets
                .ToFrozenDictionary(x => x.FullAssetPath);
        }

        public int Count => this.assets.Length;

        public IModAsset this[string fullAssetPath]
        {
            get => this.assetsByAssetPath[fullAssetPath];
        }

        bool ICollection<IModAsset>.IsReadOnly => true;
        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => throw new NotSupportedException();

        void ICollection<IModAsset>.Add(IModAsset item)
            => throw new NotSupportedException();

        void ICollection<IModAsset>.Clear()
            => throw new NotSupportedException();

        bool ICollection<IModAsset>.Contains(IModAsset item)
        {
            return this.assets.Contains(item);
        }

        public bool ContainsAsset([NotNullWhen(true)] string? fullAssetPath)
        {
            return fullAssetPath is not null &&
                this.assetsByAssetPath.ContainsKey(fullAssetPath);
        }

        void ICollection<IModAsset>.CopyTo(IModAsset[] array, int arrayIndex)
        {
            this.assets.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.assets).CopyTo(array, index);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<IModAsset> IEnumerable<IModAsset>.GetEnumerator()
        {
            return ((IEnumerable<IModAsset>)this.assets).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.assets).GetEnumerator();
        }

        bool ICollection<IModAsset>.Remove(IModAsset item)
            => throw new NotSupportedException();

        public bool TryGetAsset([NotNullWhen(true)] string? fullAssetPath, [NotNullWhen(true)] out IModAsset? asset)
        {
            if (fullAssetPath is null)
            {
                asset = default;
                return false;
            }

            return this.assetsByAssetPath.TryGetValue(fullAssetPath, out asset);
        }

        public readonly struct Enumerator
        {
            private readonly ImmutableArray<IModAsset>.Enumerator enumerator;

            internal Enumerator(AssetCollection collection)
            {
                this.enumerator = collection.assets.GetEnumerator();
            }

            public IModAsset Current
                => this.enumerator.Current;

            public bool MoveNext()
                => this.enumerator.MoveNext();
        }
    }

    public sealed class AssetCollection<TAsset> :
        IReadOnlyCollection<TAsset>,
        ICollection<TAsset>,
        ICollection,
        IEnumerable<TAsset>,
        IEnumerable
        where TAsset : notnull, IModAsset
    {
        private readonly FrozenDictionary<string, TAsset> assetsByAssetPath;
        private readonly ImmutableArray<TAsset> assets;

        internal AssetCollection(ImmutableArray<TAsset> assets)
        {
            this.assets = assets;
            this.assetsByAssetPath = assets
                .ToFrozenDictionary(x => x.AssetPath);
        }

        public int Count => this.assets.Length;

        public TAsset this[string assetPath]
        {
            get => this.assetsByAssetPath[assetPath];
        }

        bool ICollection<TAsset>.IsReadOnly => true;
        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => throw new NotSupportedException();

        void ICollection<TAsset>.Add(TAsset item)
            => throw new NotSupportedException();

        void ICollection<TAsset>.Clear()
            => throw new NotSupportedException();

        bool ICollection<TAsset>.Contains(TAsset item)
        {
            return this.assets.Contains(item);
        }

        public bool ContainsAsset([NotNullWhen(true)] string? assetPath)
        {
            return assetPath is not null &&
                this.assetsByAssetPath.ContainsKey(assetPath);
        }

        void ICollection<TAsset>.CopyTo(TAsset[] array, int arrayIndex)
        {
            this.assets.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.assets).CopyTo(array, index);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<TAsset> IEnumerable<TAsset>.GetEnumerator()
        {
            return ((IEnumerable<TAsset>)this.assets).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.assets).GetEnumerator();
        }

        bool ICollection<TAsset>.Remove(TAsset item)
            => throw new NotSupportedException();

        public bool TryGetAsset([NotNullWhen(true)] string? assetPath, [NotNullWhen(true)] out TAsset? asset)
        {
            if (assetPath is null)
            {
                asset = default;
                return false;
            }

            return this.assetsByAssetPath.TryGetValue(assetPath, out asset);
        }

        public readonly struct Enumerator
        {
            private readonly ImmutableArray<TAsset>.Enumerator enumerator;

            internal Enumerator(AssetCollection<TAsset> collection)
            {
                this.enumerator = collection.assets.GetEnumerator();
            }

            public TAsset Current
                => this.enumerator.Current;

            public bool MoveNext()
                => this.enumerator.MoveNext();
        }
    }
}
