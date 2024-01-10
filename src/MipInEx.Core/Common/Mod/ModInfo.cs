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

    public AssetCollection Assets => this.assets;
    public AssetCollection<ModAssemblyInfo> Assemblies => this.assemblies;
    public AssetCollection<ModAssetBundleInfo> AssetBundles => this.assetBundles;

    /// <inheritdoc cref="Mod.Incompatibilities"/>
    public IReadOnlyList<ModInfo> Incompatibilities => this.incompatibilities;

    /// <inheritdoc cref="Mod.MissingDependencies"/>
    public IReadOnlyList<ModDependencyInfo> MissingDependencies => this.missingDependencies;

    /// <inheritdoc cref="Mod.RequiredDependencies"/>
    public IReadOnlyList<ModInfo> RequiredDependencies => this.requiredDependencies;

    /// <inheritdoc cref="Mod.Dependencies"/>
    public IReadOnlyList<ModInfo> Dependencies => this.dependencies;

    /// <summary>
    /// Whether or not this mod is loaded.
    /// </summary>
    public bool IsLoaded => false;

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

    /// <inheritdoc cref="Mod.ToString()"/>
    public sealed override string ToString()
    {
        return $"{this.Guid} v{this.Version} by {this.Author}";
    }

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

        public int Count => this.assets.Length;

        public IModAssetInfo this[string fullAssetPath]
        {
            get => this.assetsByAssetPath[fullAssetPath];
        }

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

        public bool TryGetAsset([NotNullWhen(true)] string? fullAssetPath, [NotNullWhen(true)] out IModAssetInfo? asset)
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
            private readonly ImmutableArray<IModAssetInfo>.Enumerator enumerator;

            internal Enumerator(AssetCollection collection)
            {
                this.enumerator = collection.assets.GetEnumerator();
            }

            public IModAssetInfo Current
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
