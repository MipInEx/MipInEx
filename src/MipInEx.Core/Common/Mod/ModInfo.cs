using MipInEx.Logging;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MipInEx;

/// <summary>
/// Represents a mod.
/// </summary>
public sealed class ModInfo
{
    private ModState state;

    private readonly string name;
    private readonly string guid;
    private readonly Version version;
    private readonly string author;

    private readonly AssetCollection assets;
    private readonly AssetCollection<ModAssemblyInfo> assemblies;
    private readonly AssetCollection<ModAssetBundleInfo> assetBundles;

    private ImmutableArray<ModInfo> incompatibilities;
    private ImmutableArray<ModDependencyInfo> missingDependencies;
    private ImmutableArray<ModInfo> requiredDependencies;
    private ImmutableArray<ModInfo> dependencies;

    internal ModInfo(Mod mod)
    {
        this.state = ModState.Unloaded;

        this.name = mod.Manifest.Name;
        this.guid = mod.Manifest.Guid;
        this.version = mod.Manifest.Version;
        this.author = mod.Manifest.Author;

        this.incompatibilities = ImmutableArray<ModInfo>.Empty;
        this.missingDependencies = ImmutableArray<ModDependencyInfo>.Empty;
        this.requiredDependencies = ImmutableArray<ModInfo>.Empty;
        this.dependencies = ImmutableArray<ModInfo>.Empty;

        if (mod.Assemblies.Count > 0)
        {
            ImmutableArray<ModAssemblyInfo>.Builder assembliesBuilder = ImmutableArray.CreateBuilder<ModAssemblyInfo>();
            
            foreach (ModAssembly modAssembly in mod.Assemblies)
            {
                ModAssemblyInfo modAssemblyInfo = modAssembly.Info;
                modAssemblyInfo.Initialize(this);

                assembliesBuilder.Add(modAssemblyInfo);
            }

            this.assemblies = new AssetCollection<ModAssemblyInfo>(assembliesBuilder.ToImmutable());
        }
        else
        {
            this.assemblies = new AssetCollection<ModAssemblyInfo>(ImmutableArray<ModAssemblyInfo>.Empty);
        }

        if (mod.AssetBundles.Count > 0)
        {
            ImmutableArray<ModAssetBundleInfo>.Builder assetBundlesBuilder = ImmutableArray.CreateBuilder<ModAssetBundleInfo>();

            foreach (ModAssetBundle modAssetBundle in mod.AssetBundles)
            {
                ModAssetBundleInfo modAssetBundleInfo = modAssetBundle.Info;
                modAssetBundleInfo.Initialize(this);

                assetBundlesBuilder.Add(modAssetBundleInfo);
            }

            this.assetBundles = new AssetCollection<ModAssetBundleInfo>(assetBundlesBuilder.ToImmutable());
        }
        else
        {
            this.assetBundles = new AssetCollection<ModAssetBundleInfo>(ImmutableArray<ModAssetBundleInfo>.Empty);
        }

        this.assets = new AssetCollection(
            mod.Assets
                .Select(x => x.Info)
                .ToImmutableArray());
    }

    /// <inheritdoc cref="Mod.Name"/>
    public string Name => this.name;
    /// <inheritdoc cref="Mod.Guid"/>
    public string Guid => this.guid;
    /// <inheritdoc cref="Mod.Version"/>
    public Version Version => this.version;
    /// <inheritdoc cref="Mod.Author"/>
    public string Author => this.author;

    /// <inheritdoc cref="Mod.Assets"/>
    public AssetCollection Assets => this.assets;
    /// <inheritdoc cref="Mod.Assemblies"/>
    public AssetCollection<ModAssemblyInfo> Assemblies => this.assemblies;
    /// <inheritdoc cref="Mod.AssetBundles"/>
    public AssetCollection<ModAssetBundleInfo> AssetBundles => this.assetBundles;

    /// <inheritdoc cref="Mod.Incompatibilities"/>
    public IReadOnlyList<ModInfo> Incompatibilities => this.incompatibilities;

    /// <inheritdoc cref="Mod.MissingDependencies"/>
    public IReadOnlyList<ModDependencyInfo> MissingDependencies => this.missingDependencies;

    /// <inheritdoc cref="Mod.RequiredDependencies"/>
    public IReadOnlyList<ModInfo> RequiredDependencies => this.requiredDependencies;

    /// <inheritdoc cref="Mod.Dependencies"/>
    public IReadOnlyList<ModInfo> Dependencies => this.dependencies;

    /// <inheritdoc cref="Mod.State"/>
    public ModState State => this.state;

    /// <inheritdoc cref="Mod.IsLoaded"/>
    public bool IsLoaded => this.state == ModState.Loaded;

    /// <inheritdoc cref="Mod.IsUnloaded"/>
    public bool IsUnloaded => this.state == ModState.Unloaded;

    /// <inheritdoc cref="Mod.ToString()"/>
    public sealed override string ToString()
    {
        return $"{this.Guid} v{this.Version} by {this.Author}";
    }

    internal void RefreshIncompatibilitiesAndDependencies(
        ImmutableArray<Mod> incompatibilities,
        ImmutableArray<ModDependencyInfo> missingDependencies,
        ImmutableArray<Mod> requiredDependencies,
        ImmutableArray<Mod> dependencies)
    {
        this.incompatibilities = incompatibilities
            .Select(x => x.Info)
            .ToImmutableArray();
        this.missingDependencies = missingDependencies;
        this.requiredDependencies = requiredDependencies
            .Select(x => x.Info)
            .ToImmutableArray();
        this.dependencies = dependencies
            .Select(x => x.Info)
            .ToImmutableArray();
    }

    internal void SetState(ModState state)
    {
        this.state = state;
    }

    /// <inheritdoc cref="Mod.AssetCollection"/>
    public sealed class AssetCollection :
        IReadOnlyCollection<IModAssetInfo>,
        ICollection<IModAssetInfo>,
        ICollection,
        IEnumerable<IModAssetInfo>,
        IEnumerable
    {
        private readonly FrozenDictionary<string, IModAssetInfo> assetsByAssetPath;
        private readonly ImmutableArray<IModAssetInfo> assets;

        internal AssetCollection(ImmutableArray<IModAssetInfo> assets)
        {
            this.assets = assets;
            if (assets.Length == 0)
            {
                this.assetsByAssetPath = FrozenDictionary<string, IModAssetInfo>.Empty;
                return;
            }

            this.assetsByAssetPath = assets
                .ToFrozenDictionary(
                    x => x.FullAssetPath,
                    x => x);
        }

        /// <inheritdoc cref="Mod.AssetCollection.Count"/>
        public int Count
            => this.assets.Length;

        /// <inheritdoc cref="Mod.AssetCollection.this[int]"/>
        public IModAssetInfo this[int index]
            => this.assets[index];

        /// <inheritdoc cref="Mod.AssetCollection.this[string]"/>
        public IModAssetInfo this[string fullAssetPath]
            => this.assetsByAssetPath[fullAssetPath];

        bool ICollection<IModAssetInfo>.IsReadOnly => true;
        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => throw new NotSupportedException();

        void ICollection<IModAssetInfo>.Add(IModAssetInfo item)
            => throw new NotSupportedException();

        void ICollection<IModAssetInfo>.Clear()
            => throw new NotSupportedException();

        bool ICollection<IModAssetInfo>.Contains(IModAssetInfo item)
        {
            return this.assets.Contains(item);
        }

        /// <inheritdoc cref="Mod.AssetCollection.ContainsAsset(string?)"/>
        public bool ContainsAsset([NotNullWhen(true)] string? fullAssetPath)
        {
            return fullAssetPath is not null &&
                this.assetsByAssetPath.ContainsKey(fullAssetPath);
        }

        void ICollection<IModAssetInfo>.CopyTo(IModAssetInfo[] array, int arrayIndex)
        {
            this.assets.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.assets).CopyTo(array, index);
        }

        /// <inheritdoc cref="Mod.AssetCollection.GetEnumerator()"/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<IModAssetInfo> IEnumerable<IModAssetInfo>.GetEnumerator()
        {
            return ((IEnumerable<IModAssetInfo>)this.assets).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.assets).GetEnumerator();
        }

        bool ICollection<IModAssetInfo>.Remove(IModAssetInfo item)
            => throw new NotSupportedException();

        /// <inheritdoc cref="Mod.AssetCollection.TryGetAsset(string?, out IModAsset?)"/>
        public bool TryGetAsset([NotNullWhen(true)] string? fullAssetPath, [NotNullWhen(true)] out IModAssetInfo? asset)
        {
            if (fullAssetPath is null)
            {
                asset = default;
                return false;
            }

            return this.assetsByAssetPath.TryGetValue(fullAssetPath, out asset);
        }

        /// <inheritdoc cref="Mod.AssetCollection.Enumerator"/>
        public readonly struct Enumerator
        {
            private readonly ImmutableArray<IModAssetInfo>.Enumerator enumerator;

            internal Enumerator(AssetCollection collection)
            {
                this.enumerator = collection.assets.GetEnumerator();
            }

            /// <inheritdoc cref="Mod.AssetCollection.Enumerator.Current"/>
            public IModAssetInfo Current
                => this.enumerator.Current;

            /// <inheritdoc cref="Mod.AssetCollection.Enumerator.MoveNext()"/>
            public bool MoveNext()
                => this.enumerator.MoveNext();
        }
    }

    /// <inheritdoc cref="Mod.AssetCollection{TAsset}"/>
    public sealed class AssetCollection<TAsset> :
        IReadOnlyCollection<TAsset>,
        ICollection<TAsset>,
        ICollection,
        IEnumerable<TAsset>,
        IEnumerable
        where TAsset : notnull, IModAssetInfo
    {
        private readonly FrozenDictionary<string, TAsset> assetsByAssetPath;
        private readonly ImmutableArray<TAsset> assets;

        internal AssetCollection(ImmutableArray<TAsset> assets)
        {
            this.assets = assets;
            if (assets.Length == 0)
            {
                this.assetsByAssetPath = FrozenDictionary<string, TAsset>.Empty;
                return;
            }

            this.assetsByAssetPath = assets
                .ToFrozenDictionary(x => x.AssetPath);
        }

        /// <inheritdoc cref="Mod.AssetCollection{TAsset}.Count"/>
        public int Count
            => this.assets.Length;

        /// <inheritdoc cref="Mod.AssetCollection{TAsset}.this[int]"/>
        public IModAssetInfo this[int index]
            => this.assets[index];

        /// <inheritdoc cref="Mod.AssetCollection{TAsset}.this[string]"/>
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

        /// <inheritdoc cref="Mod.AssetCollection{TAsset}.ContainsAsset(string?)"/>
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

        /// <inheritdoc cref="Mod.AssetCollection{TAsset}.GetEnumerator()"/>
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

        /// <inheritdoc cref="Mod.AssetCollection{TAsset}.TryGetAsset(string?, out TAsset)"/>
        public bool TryGetAsset([NotNullWhen(true)] string? assetPath, [NotNullWhen(true)] out TAsset? asset)
        {
            if (assetPath is null)
            {
                asset = default;
                return false;
            }

            return this.assetsByAssetPath.TryGetValue(assetPath, out asset);
        }

        /// <inheritdoc cref="Mod.AssetCollection{TAsset}.Enumerator"/>
        public readonly struct Enumerator
        {
            private readonly ImmutableArray<TAsset>.Enumerator enumerator;

            internal Enumerator(AssetCollection<TAsset> collection)
            {
                this.enumerator = collection.assets.GetEnumerator();
            }

            /// <inheritdoc cref="Mod.AssetCollection{TAsset}.Enumerator.Current"/>
            public TAsset Current
                => this.enumerator.Current;


            /// <inheritdoc cref="Mod.AssetCollection{TAsset}.Enumerator.MoveNext()"/>
            public bool MoveNext()
                => this.enumerator.MoveNext();
        }
    }
}
