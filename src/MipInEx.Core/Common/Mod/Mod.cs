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

    private bool isCircularDependency;

    private LoadAsyncOperation? loadOperation;
    private UnloadAsyncOperation? unloadOperation;

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

    public ModInfo Info => this.info;

    /// <summary>
    /// The state of this mod.
    /// </summary>
    public ModState State => this.info.State;

    /// <summary>
    /// Whether or not this mod is loaded.
    /// </summary>
    public bool IsLoaded => this.info.IsLoaded;

    /// <summary>
    /// Whether or not this mod is unloaded.
    /// </summary>
    public bool IsUnloaded => this.info.IsUnloaded;

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

    internal void Load()
    {
        if (this.IsLoaded)
            return;
        else if (this.unloadOperation != null)
            throw new InvalidOperationException("Cannot load whilst the mod is being unloaded!");
        else if (this.loadOperation != null)
            throw new InvalidOperationException("An existing async load request is already active!");

        this.info.SetState(ModState.Loading);
        List<Exception> exceptions = new();

        foreach (IModAsset asset in this.assets)
        {
            if (asset.IsLoaded || asset.Manifest.LoadManually)
            {
                continue;
            }

            try
            {
                asset.Load();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        this.info.SetState(ModState.Loaded);
        if (exceptions.Count > 0)
        {
            throw new AggregateException("Exceptions occurred whilst loading mod", exceptions);
        }
    }

    internal ModAsyncOperation LoadAsync()
    {
        if (this.IsLoaded)
            return ModAsyncOperation.Completed;
        else if (this.unloadOperation != null)
            return ModAsyncOperation.FromException(new InvalidOperationException("Cannot load whilst the mod is being unloaded!"));

        this.loadOperation ??= new LoadAsyncOperation(this);
        return this.loadOperation;
    }

    internal void Unload()
    {
        if (this.IsUnloaded)
            return;
        else if (this.loadOperation != null)
            throw new InvalidOperationException("Cannot unload whilst the mod is being loaded!");
        else if (this.unloadOperation != null)
            throw new InvalidOperationException("An existing async unload request is already active!");

        this.info.SetState(ModState.Unloading);

        for (int index = this.assets.Count - 1; index >= 0; index--)
        {
            IModAsset asset = this.assets[index];
            if (!asset.IsLoaded)
            {
                continue;
            }

            asset.Unload();

        }

        this.info.SetState(ModState.Unloaded);
    }

    internal ModAsyncOperation UnloadAsync()
    {
        if (this.IsUnloaded)
            return ModAsyncOperation.Completed;
        else if (this.loadOperation != null)
            return ModAsyncOperation.FromException(new InvalidOperationException("Cannot unload whilst the mod is being loaded!"));

        this.unloadOperation ??= new UnloadAsyncOperation(this);
        return this.unloadOperation;
    }

    /// <summary>
    /// The collection containing all assets in a mod.
    /// </summary>
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

        /// <summary>
        /// The number of assets in this collection.
        /// </summary>
        public int Count
            => this.assets.Length;

        /// <summary>
        /// Gets the asset at the specified index.
        /// </summary>
        /// <param name="index">
        /// The index of the mod asset.
        /// </param>
        /// <returns>
        /// The mod asset at the specified index.
        /// </returns>
        public IModAsset this[int index]
            => this.assets[index];

        /// <summary>
        /// Gets the asset with the specified full asset path.
        /// </summary>
        /// <param name="fullAssetPath">
        /// The full asset path of the asset to get.
        /// </param>
        /// <returns>
        /// The asset with the specified full asset path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fullAssetPath"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// No asset was found from the specified
        /// <paramref name="fullAssetPath"/>.
        /// <paramref name="fullAssetPath"/>.
        /// </exception>
        public IModAsset this[string fullAssetPath]
            => this.assetsByAssetPath[fullAssetPath];

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

        /// <summary>
        /// Determines whether or not this asset collection
        /// contains an asset at the specified
        /// <paramref name="fullAssetPath"/>.
        /// </summary>
        /// <param name="fullAssetPath">
        /// The full asset path of the asset to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an asset exists at
        /// <paramref name="fullAssetPath"/>;
        /// <see langword="false"/>, otherwise.
        /// </returns>
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

        /// <summary>
        /// Gets an enumerator to enumerate through this asset
        /// collection.
        /// </summary>
        /// <remarks>
        /// The returned enumerator struct does not implement
        /// <see cref="IEnumerator{T}"/> to improve
        /// performance due to not needing to dispose the
        /// enumerator. However, the methods on the enumerator
        /// struct allow it to be used in a
        /// <see langword="foreach"/> loop.
        /// </remarks>
        /// <returns>
        /// An enumerator to enumerate through this asset
        /// collection.
        /// </returns>
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


        /// <summary>
        /// Attempts to get the asset at the specified
        /// <paramref name="fullAssetPath"/>.
        /// </summary>
        /// <param name="fullAssetPath">
        /// The full asset path of the asset to get.
        /// </param>
        /// <param name="asset">
        /// If this method returns <see langword="true"/>, then
        /// the value will be the found asset, otherwise if
        /// this method returns <see langword="false"/>, then
        /// the value will be <see langword="default"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this collection contains
        /// an asset at <paramref name="fullAssetPath"/>;
        /// <see langword="false"/>, otherwise.
        /// </returns>
        public bool TryGetAsset([NotNullWhen(true)] string? fullAssetPath, [NotNullWhen(true)] out IModAsset? asset)
        {
            if (fullAssetPath is null)
            {
                asset = default;
                return false;
            }

            return this.assetsByAssetPath.TryGetValue(fullAssetPath, out asset);
        }

        /// <summary>
        /// An enumerator that enumerates through an asset
        /// collection.
        /// </summary>
        public readonly struct Enumerator
        {
            private readonly ImmutableArray<IModAsset>.Enumerator enumerator;

            internal Enumerator(AssetCollection collection)
            {
                this.enumerator = collection.assets.GetEnumerator();
            }

            /// <summary>
            /// The current mod asset.
            /// </summary>
            public IModAsset Current
                => this.enumerator.Current;

            /// <summary>
            /// Advances to the next mod asset.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if another mod asset
            /// exists in the collection;
            /// <see langword="false"/>, otherwise.
            /// </returns>
            public bool MoveNext()
                => this.enumerator.MoveNext();
        }
    }

    /// <summary>
    /// The collection containing assets of a specified type in
    /// a mod.
    /// </summary>
    /// <typeparam name="TAsset">
    /// The type of asset this collection holds.
    /// </typeparam>
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

        /// <summary>
        /// The number of assets in this collection.
        /// </summary>
        public int Count
            => this.assets.Length;

        /// <summary>
        /// Gets the asset at the specified index.
        /// </summary>
        /// <param name="index">
        /// The index of the mod asset.
        /// </param>
        /// <returns>
        /// The mod asset at the specified index.
        /// </returns>
        public TAsset this[int index]
            => this.assets[index];

        /// <summary>
        /// Gets the asset with the specified asset path.
        /// </summary>
        /// <param name="assetPath">
        /// The asset path of the asset to get.
        /// </param>
        /// <returns>
        /// The asset with the specified asset path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="assetPath"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// No asset was found from the specified
        /// <paramref name="assetPath"/>.
        /// </exception>
        public TAsset this[string assetPath]
            => this.assetsByAssetPath[assetPath];

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

        /// <summary>
        /// Determines whether or not this asset collection
        /// contains an asset at the specified
        /// <paramref name="assetPath"/>.
        /// </summary>
        /// <param name="assetPath">
        /// The asset path of the asset to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an asset exists at
        /// <paramref name="assetPath"/>;
        /// <see langword="false"/>, otherwise.
        /// </returns>
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

        /// <summary>
        /// Gets an enumerator to enumerate through this asset
        /// collection.
        /// </summary>
        /// <remarks>
        /// The returned enumerator struct does not implement
        /// <see cref="IEnumerator{T}"/> to improve
        /// performance due to not needing to dispose the
        /// enumerator. However, the methods on the enumerator
        /// struct allow it to be used in a
        /// <see langword="foreach"/> loop.
        /// </remarks>
        /// <returns>
        /// An enumerator to enumerate through this asset
        /// collection.
        /// </returns>
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

        /// <summary>
        /// Attempts to get the asset at the specified
        /// <paramref name="assetPath"/>.
        /// </summary>
        /// <param name="assetPath">
        /// The asset path of the asset to get.
        /// </param>
        /// <param name="asset">
        /// If this method returns <see langword="true"/>, then
        /// the value will be the found asset, otherwise if
        /// this method returns <see langword="false"/>, then
        /// the value will be <see langword="default"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this collection contains
        /// an asset at <paramref name="assetPath"/>;
        /// <see langword="false"/>, otherwise.
        /// </returns>
        public bool TryGetAsset([NotNullWhen(true)] string? assetPath, [NotNullWhen(true)] out TAsset? asset)
        {
            if (assetPath is null)
            {
                asset = default;
                return false;
            }

            return this.assetsByAssetPath.TryGetValue(assetPath, out asset);
        }

        /// <summary>
        /// An enumerator that enumerates through an asset
        /// collection.
        /// </summary>
        public readonly struct Enumerator
        {
            private readonly ImmutableArray<TAsset>.Enumerator enumerator;

            internal Enumerator(AssetCollection<TAsset> collection)
            {
                this.enumerator = collection.assets.GetEnumerator();
            }

            /// <summary>
            /// The current mod asset.
            /// </summary>
            public TAsset Current
                => this.enumerator.Current;

            /// <summary>
            /// Advances to the next mod asset.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if another mod asset
            /// exists in the collection;
            /// <see langword="false"/>, otherwise.
            /// </returns>
            public bool MoveNext()
                => this.enumerator.MoveNext();
        }
    }

    private sealed class LoadAsyncOperation : ModAsyncMultiOperation
    {
        private readonly Mod mod;
        private readonly ImmutableArray<IModAsset> allAssets;
        private readonly List<Exception> allExceptions;
        private ModAsyncOperation? currentOperation;
        private int index;
        private Exception? rootException;

        public LoadAsyncOperation(Mod mod)
        {
            this.mod = mod;
            this.allAssets = mod.assets
                .Where(x => !x.Manifest.LoadManually)
                .ToImmutableArray();
            this.allExceptions = new();

            this.index = -1;
            this.currentOperation = null;
            this.rootException = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.index == -1)
                    return ModAsyncOperationStatus.NotStarted;
                else if (this.index >= this.allAssets.Length)
                {
                    if (this.rootException is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                else
                    return ModAsyncOperationStatus.Running;
            }
        }
        public sealed override bool IsRunning => this.index > -1 && this.index < this.allAssets.Length;
        public sealed override bool IsCompleted => this.index >= this.allAssets.Length;
        public sealed override bool IsCompletedSuccessfully => this.index >= this.allAssets.Length && this.rootException is null;
        public sealed override bool IsFaulted => this.index >= this.allAssets.Length && this.rootException is not null;
        public sealed override Exception? Exception => this.rootException;

        public sealed override int OperationCount
            => this.allAssets.Length;
        public sealed override int CompletedOperationCount
            => Math.Clamp(0, this.index, this.allAssets.Length);


        public sealed override double GetTotalProgress()
        {
            if (this.index < 0)
                return 0.0;
            else if (this.allAssets.Length == 0)
                return 1.0;
            else
                return this.index + (this.currentOperation?.GetProgress() ?? 0.0);
        }

        public sealed override string? GetDescriptionString()
        {
            return this.currentOperation?.GetDescriptionString();
        }

        public sealed override double GetProgress()
        {
            if (this.index < 0)
                return 0.0;
            else if (this.allAssets.Length == 0)
                return 1.0;
            else
                return (this.index + (this.currentOperation?.GetProgress() ?? 0.0)) / this.allAssets.Length;
        }

        public sealed override bool Process()
        {
            if (this.index >= this.allAssets.Length)
            {
                return true;
            }

            if (this.index < 0)
            {
                this.mod.info.SetState(ModState.Loading);
                this.index++;
                if (this.allAssets.Length == 0)
                {
                    this.mod.info.SetState(ModState.Loaded);
                    this.mod.loadOperation = null;
                    return true;
                }
            }

            if (this.currentOperation == null)
            {
                this.currentOperation = this.allAssets[this.index].LoadAsync();
                return false;
            }

            if (!this.currentOperation.Process())
            {
                return false;
            }

            if (this.currentOperation.IsFaulted)
            {
                Exception? ex = this.currentOperation.Exception;
                if (ex != null)
                {
                    this.allExceptions.Add(ex);
                }
            }

            this.currentOperation = null;
            this.index++;

            if (this.index >= this.allAssets.Length)
            {
                this.mod.info.SetState(ModState.Loaded);
                if (this.allExceptions.Count > 0)
                {
                    this.rootException = new AggregateException("Exceptions occurred whilst loading mod", this.allExceptions);
                }

                this.mod.loadOperation = null;
                return true;
            }

            this.currentOperation = this.allAssets[this.index].LoadAsync();
            return false;
        }
    }

    private sealed class UnloadAsyncOperation : ModAsyncMultiOperation
    {
        private readonly Mod mod;
        private readonly ImmutableArray<IModAsset> allAssets;
        private readonly List<Exception> allExceptions;
        private ModAsyncOperation? currentOperation;
        private int index;
        private Exception? rootException;

        public UnloadAsyncOperation(Mod mod)
        {
            this.mod = mod;
            this.allAssets = mod.assets
                .Reverse()
                .ToImmutableArray();
            this.allExceptions = new();

            this.index = -1;
            this.currentOperation = null;
            this.rootException = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.index == -1)
                    return ModAsyncOperationStatus.NotStarted;
                else if (this.index >= this.allAssets.Length)
                {
                    if (this.rootException is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                else
                    return ModAsyncOperationStatus.Running;
            }
        }
        public sealed override bool IsRunning => this.index > -1 && this.index < this.allAssets.Length;
        public sealed override bool IsCompleted => this.index >= this.allAssets.Length;
        public sealed override bool IsCompletedSuccessfully => this.index >= this.allAssets.Length && this.rootException is null;
        public sealed override bool IsFaulted => this.index >= this.allAssets.Length && this.rootException is not null;
        public sealed override Exception? Exception => this.rootException;

        public sealed override int OperationCount
            => this.allAssets.Length;
        public sealed override int CompletedOperationCount
            => Math.Clamp(0, this.index, this.allAssets.Length);


        public sealed override double GetTotalProgress()
        {
            if (this.index < 0)
                return 0.0;
            else if (this.allAssets.Length == 0)
                return 1.0;
            else
                return this.index + (this.currentOperation?.GetProgress() ?? 0.0);
        }

        public sealed override string? GetDescriptionString()
        {
            return this.currentOperation?.GetDescriptionString();
        }

        public sealed override double GetProgress()
        {
            if (this.index < 0)
                return 0.0;
            else if (this.allAssets.Length == 0)
                return 1.0;
            else
                return (this.index + (this.currentOperation?.GetProgress() ?? 0.0)) / this.allAssets.Length;
        }

        public sealed override bool Process()
        {
            if (this.index >= this.allAssets.Length)
            {
                return true;
            }

            if (this.index < 0)
            {
                this.mod.info.SetState(ModState.Unloading);
                this.index++;
                if (this.allAssets.Length == 0)
                {
                    this.mod.info.SetState(ModState.Unloaded);
                    this.mod.unloadOperation = null;
                    return true;
                }
            }

            if (this.currentOperation == null)
            {
                this.currentOperation = this.allAssets[this.index].UnloadAsync();
                return false;
            }

            if (!this.currentOperation.Process())
            {
                return false;
            }

            if (this.currentOperation.IsFaulted)
            {
                Exception? ex = this.currentOperation.Exception;
                if (ex != null)
                {
                    this.allExceptions.Add(ex);
                }
            }

            this.currentOperation = null;
            this.index++;

            if (this.index >= this.allAssets.Length)
            {
                this.mod.info.SetState(ModState.Unloaded);
                if (this.allExceptions.Count > 0)
                {
                    this.rootException = new AggregateException("Exceptions occurred whilst unloading mod", this.allExceptions);
                }

                this.mod.unloadOperation = null;
                return true;
            }

            this.currentOperation = this.allAssets[this.index].UnloadAsync();
            return false;
        }
    }
}
