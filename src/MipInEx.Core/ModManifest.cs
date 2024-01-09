using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// The representation of a mod's manifest.
/// </summary>
public sealed class ModManifest : IEquatable<ModManifest>
{
    private readonly string guid;
    private readonly string name;
    private readonly string description;
    private readonly string author;
    private readonly Version version;
    private readonly FrozenDictionary<string, IModAssetManifest> assetsByAssetPath;
    private readonly FrozenDictionary<string, ModAssemblyManifest> assemblyAssetsByAssetPath;
    private readonly FrozenDictionary<string, ModAssetBundleManifest> assetBundleAssetsByAssetPath;
    private readonly ImmutableArray<ModDependencyInfo> dependencies;
    private readonly ImmutableArray<ModIncompatibilityInfo> incompatibilities;

    internal ModManifest(
        string guid,
        string name,
        string description,
        string author,
        Version version,
        ImmutableArray<IModAssetManifest> assets,
        ImmutableArray<ModDependencyInfo> dependencies,
        ImmutableArray<ModIncompatibilityInfo> incompatibilities)
    {
        this.guid = guid;
        this.name = name;
        this.description = description;
        this.author = author;
        this.version = version;
        this.assetsByAssetPath = assets
            .ToFrozenDictionary(x => x.LongAssetPath);
        this.assemblyAssetsByAssetPath = assets
            .OfType<ModAssemblyManifest>()
            .ToFrozenDictionary(x => x.AssetPath);
        this.assetBundleAssetsByAssetPath = assets
            .OfType<ModAssetBundleManifest>()
            .ToFrozenDictionary(x => x.AssetPath);
        this.dependencies = dependencies;
        this.incompatibilities = incompatibilities;
    }

    /// <summary>
    /// The globally unique identifier of the mod.
    /// </summary>
    /// <remarks>
    /// Must be between 1 and 256 characters (inclusive) in
    /// length, and can only contain letters, numbers, periods,
    /// underscores, and dashes.
    /// </remarks>
    public string Guid => this.guid;

    /// <summary>
    /// The name of the mod.
    /// </summary>
    /// <remarks>
    /// Must be between 1 and 256 characters (inclusive).
    /// Leading and trailing whitespace are automatically
    /// removed.
    /// </remarks>
    public string Name => this.name;

    /// <summary>
    /// The short description of the mod.
    /// </summary>
    /// <remarks>
    /// Must be between 0 and 512 characters (inclusive).
    /// Leading and trailing whitespace are automatically
    /// removed.
    /// </remarks>
    public string Description => this.description;

    /// <summary>
    /// The author of the mod.
    /// </summary>
    /// <remarks>
    /// Must be between 1 and 256 characters (inclusive).
    /// Leading and trailing whitespace are automatically
    /// removed.
    /// </remarks>
    public string Author => this.author;

    /// <summary>
    /// The version of this mod.
    /// </summary>
    public Version Version => this.version;

    /// <summary>
    /// A readonly list of assets for this mod.
    /// </summary>
    /// <remarks>
    /// All assets don't need to be specified. Unspecified
    /// assets will have their settings set to default, and
    /// a load priority of 0.
    /// </remarks>
    public IReadOnlyList<IModAssetManifest> Assets => this.assetsByAssetPath.Values;

    /// <summary>
    /// A readonly list of dependencies for this mod.
    /// </summary>
    public IReadOnlyList<ModDependencyInfo> Dependencies => this.dependencies;

    /// <summary>
    /// A readonly list of incompatibilities for this mod.
    /// </summary>
    public IReadOnlyList<ModIncompatibilityInfo> Incompatibilities => this.incompatibilities;

    /// <summary>
    /// Attempts to get an assembly asset in this mod manifest
    /// with the specified <paramref name="assetPath"/>.
    /// </summary>
    /// <param name="assetPath">
    /// The asset path of the assembly asset.
    /// </param>
    /// <param name="assemblyAsset">
    /// The found assembly asset.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the assembly asset was found;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetAssemblyAsset([NotNullWhen(true)] string? assetPath, [NotNullWhen(true)] out ModAssemblyManifest? assemblyAsset)
    {
        if (assetPath is null)
        {
            assemblyAsset = null;
            return false;
        }
        else
        {
            return this.assemblyAssetsByAssetPath.TryGetValue(assetPath, out assemblyAsset);
        }
    }

    /// <summary>
    /// Attempts to get an asset bundle asset in this mod
    /// manifest with the specified
    /// <paramref name="assetPath"/>.
    /// </summary>
    /// <param name="assetPath">
    /// The asset path of the asset bundle asset.
    /// </param>
    /// <param name="assetBundleAsset">
    /// The found asset bundle asset.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the asset bundle asset was
    /// found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetAssetBundleAsset([NotNullWhen(true)] string? assetPath, [NotNullWhen(true)] out ModAssetBundleManifest? assetBundleAsset)
    {
        if (assetPath is null)
        {
            assetBundleAsset = null;
            return false;
        }
        else
        {
            return this.assetBundleAssetsByAssetPath.TryGetValue(assetPath, out assetBundleAsset);
        }
    }

    /// <summary>
    /// Attempts to get an asset in this mod manifest with the
    /// specified <paramref name="longAssetPath"/>.
    /// </summary>
    /// <param name="longAssetPath">
    /// The long asset path of the asset.
    /// </param>
    /// <param name="asset">
    /// The found asset.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the asset was found;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetAsset([NotNullWhen(true)] string? longAssetPath, [NotNullWhen(true)] out IModAssetManifest? asset)
    {
        if (longAssetPath is null)
        {
            asset = null;
            return false;
        }
        else
        {
            return this.assetsByAssetPath.TryGetValue(longAssetPath, out asset);
        }
    }

    /// <summary>
    /// Returns whether this mod manifest exactly equals
    /// <paramref name="other"/>.
    /// </summary>
    /// <param name="other">
    /// The other mod manifest to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this mod manifest exactly
    /// equals <paramref name="other"/>;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public bool ExactlyEquals([NotNullWhen(true)] ModManifest? other)
    {
        if (other is null) return false;
        else if (object.ReferenceEquals(this, other)) return true;
        else if (this.guid != other.guid ||
            this.name != other.name ||
            this.author != other.author ||
            this.version != other.version ||
            this.description != other.description)
        {
            return false;
        }

        int dependencyCount = this.dependencies.Length;
        if (dependencyCount != other.dependencies.Length)
        {
            return false;
        }

        int incompatibilityCount = this.incompatibilities.Length;
        if (incompatibilityCount != other.incompatibilities.Length)
        {
            return false;
        }

        for (int index = 0; index < dependencyCount; index++)
        {
            if (!this.dependencies[index].Equals(other.dependencies[index]))
            {
                return false;
            }
        }

        for (int index = 0; index < incompatibilityCount; index++)
        {
            if (!this.incompatibilities[index].Equals(other.incompatibilities[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] object? obj)
    {
        return this.Equals(obj as ModManifest);
    }

    /// <summary>
    /// Returns whether this current mod manifest is equal to
    /// the specified mod manifest.
    /// </summary>
    /// <param name="other">
    /// The mod manifest to check. 
    /// </param>
    /// <remarks>
    /// Mod manifests are equal if their
    /// <see cref="ModManifest.Guid">Guid</see>,
    /// <see cref="ModManifest.Name">Name</see>,
    /// <see cref="ModManifest.Author">Author</see>, and
    /// <see cref="ModManifest.Version">Version</see> are
    /// equal.
    /// <para>
    /// <b>
    /// <see cref="ModManifest.Description">Description</see>,
    /// <see cref="ModManifest.Dependencies">Dependencies</see>,
    /// and
    /// <see cref="ModManifest.Incompatibilities">Incompatibilities</see>
    /// are not checked.</b> To check those, use
    /// <see cref="ExactlyEquals(ModManifest?)"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> if this mod manifest equals the
    /// <paramref name="other"/> mod manifest;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public bool Equals([NotNullWhen(true)] ModManifest? other)
    {
        if (other is null) return false;
        else if (object.ReferenceEquals(this, other)) return true;
        else return this.guid == other.guid &&
                this.name == other.name &&
                this.author == other.author &&
                this.version == other.version;
    }

    /// <summary>
    /// Gets the hash code of this current mod manifest.
    /// </summary>
    /// <returns>
    /// The combine hash code of
    /// <see cref="ModManifest.Guid">Guid</see>,
    /// <see cref="ModManifest.Name">Name</see>,
    /// <see cref="ModManifest.Author">Author</see>, and
    /// <see cref="ModManifest.Version">Version</see>
    /// (in that order)
    /// </returns>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(this.guid, this.name, this.author, this.version);
    }

    /// <summary>
    /// Returns a string representation of this current mod
    /// manifest.
    /// </summary>
    /// <returns>
    /// <c>$"{<see langword="this"/>.<see cref="ModManifest.Guid">Guid</see>} v{<see langword="this"/>.<see cref="ModManifest.Version">Version</see>} by {<see langword="this"/>.<see cref="ModManifest.Author">Author</see>}"</c>
    /// </returns>
    public sealed override string ToString()
    {
        return $"{this.guid} v{this.version} by {this.author}";
    }

    /// <summary>
    /// Creates a mod manifest from the given arguments.
    /// </summary>
    /// <param name="guid">
    /// The globally unique identifier of the mod.
    /// <para>
    /// Must be between 1 and 256 characters (inclusive) in
    /// length, and can only contain letters, numbers, periods,
    /// underscores, and dashes.
    /// </para>
    /// </param>
    /// <param name="name">
    /// The name of the mod.
    /// <para>
    /// Must be between 1 and 256 characters (inclusive).
    /// Leading and trailing whitespace are automatically
    /// removed.
    /// </para>
    /// </param>
    /// <param name="description">
    /// The short description of the mod.
    /// <para>
    /// Must be between 0 and 512 characters (inclusive).
    /// Leading and trailing whitespace are automatically
    /// removed.
    /// </para>
    /// </param>
    /// <param name="author">
    /// The author of the mod.
    /// <para>
    /// Must be between 1 and 256 characters (inclusive).
    /// Leading and trailing whitespace are automatically
    /// removed.
    /// </para>
    /// </param>
    /// <param name="version">
    /// The version of the mod.
    /// </param>
    /// <param name="assets">
    /// The assets of the mod.
    /// </param>
    /// <param name="dependencies">
    /// The dependencies of the mod.
    /// </param>
    /// <param name="incompatibilities">
    /// The incompatibilities of the mod.
    /// </param>
    /// <returns>
    /// A <see cref="ModManifest"/> based on the provided
    /// arguments.
    /// </returns>
    /// <exception cref="AggregateException">
    /// An exception containing all validation exceptions of
    /// the specified arguments.
    /// </exception>
    public static ModManifest Create(string guid, string name, string description, string author, Version version, IEnumerable<IModAssetManifest> assets, IEnumerable<ModDependencyInfo> dependencies, IEnumerable<ModIncompatibilityInfo> incompatibilities)
    {
        // create list of exceptions.
        // that way, we can throw an aggregate exception that will contain
        // all errors when creating the mod.
        List<ArgumentException> exceptions = new();

        name = name?.Trim()!;
        description = description?.Trim()!;
        author = author?.Trim()!;

        ModPropertyUtil.ValidateGuid(guid, exceptions);
        ModPropertyUtil.ValidateName(name, exceptions);
        ModPropertyUtil.ValidateDescription(description, exceptions);
        ModPropertyUtil.ValidateAuthor(author, exceptions);
        ModPropertyUtil.ValidateVersion(version, exceptions);

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }

        ImmutableArray<IModAssetManifest> convertedAssets;
        int assetCount = -1;

        if (dependencies is null)
        {
            assetCount = 0;
        }
        else if (dependencies is ICollection<IModAssetManifest> assetCollection)
        {
            assetCount = assetCollection.Count;
        }
        else if (dependencies is IReadOnlyCollection<IModAssetManifest> readonlyAssetCollection)
        {
            assetCount = readonlyAssetCollection.Count;
        }

        if (assetCount == 0)
        {
            convertedAssets = ImmutableArray<IModAssetManifest>.Empty;
        }
        else
        {
            ImmutableArray<IModAssetManifest>.Builder convertedAssetBuilder = assetCount > 0 ?
                ImmutableArray.CreateBuilder<IModAssetManifest>(assetCount) :
                ImmutableArray.CreateBuilder<IModAssetManifest>();

            foreach (IModAssetManifest asset in assets!)
            {
                if (asset is null)
                    continue;

                convertedAssetBuilder.Add(asset);
            }

            if (convertedAssetBuilder.Count == 0)
            {
                convertedAssets = ImmutableArray<IModAssetManifest>.Empty;
            }
            else
            {
                convertedAssets = convertedAssetBuilder.ToImmutable();
            }
        }

        ImmutableArray<ModDependencyInfo> convertedDependencies;
        int dependencyCount = -1;

        if (dependencies is null)
        {
            dependencyCount = 0;
        }
        else if (dependencies is ICollection<ModDependencyInfo> dependencyCollection)
        {
            dependencyCount = dependencyCollection.Count;
        }
        else if (dependencies is IReadOnlyCollection<ModDependencyInfo> readonlyDependencyCollection)
        {
            dependencyCount = readonlyDependencyCollection.Count;
        }

        if (dependencyCount == 0)
        {
            convertedDependencies = ImmutableArray<ModDependencyInfo>.Empty;
        }
        else
        {
            ImmutableArray<ModDependencyInfo>.Builder convertedDependencyBuilder = dependencyCount > 0 ?
                ImmutableArray.CreateBuilder<ModDependencyInfo>(dependencyCount) :
                ImmutableArray.CreateBuilder<ModDependencyInfo>();

            foreach (ModDependencyInfo dependency in dependencies!)
            {
                if (dependency is null)
                    continue;

                convertedDependencyBuilder.Add(dependency);
            }

            if (convertedDependencyBuilder.Count == 0)
            {
                convertedDependencies = ImmutableArray<ModDependencyInfo>.Empty;
            }
            else
            {
                convertedDependencies = convertedDependencyBuilder.ToImmutable();
            }
        }

        ImmutableArray<ModIncompatibilityInfo> convertedIncompatibilities;
        int incompatibilityCount = -1;

        if (incompatibilities is null)
        {
            incompatibilityCount = 0;
        }
        else if (incompatibilities is ICollection<ModIncompatibilityInfo> incompatibilityCollection)
        {
            incompatibilityCount = incompatibilityCollection.Count;
        }
        else if (incompatibilities is IReadOnlyCollection<ModIncompatibilityInfo> readonlyIncompatibilityCollection)
        {
            incompatibilityCount = readonlyIncompatibilityCollection.Count;
        }

        if (dependencyCount == 0)
        {
            convertedIncompatibilities = ImmutableArray<ModIncompatibilityInfo>.Empty;
        }
        else
        {
            ImmutableArray<ModIncompatibilityInfo>.Builder convertedIncompatibilityBuilder = incompatibilityCount > 0 ?
                ImmutableArray.CreateBuilder<ModIncompatibilityInfo>(incompatibilityCount) :
                ImmutableArray.CreateBuilder<ModIncompatibilityInfo>();

            foreach (ModIncompatibilityInfo incompatibility in incompatibilities!)
            {
                if (incompatibility is null)
                    continue;

                convertedIncompatibilityBuilder.Add(incompatibility);
            }

            if (convertedIncompatibilityBuilder.Count == 0)
            {
                convertedIncompatibilities = ImmutableArray<ModIncompatibilityInfo>.Empty;
            }
            else
            {
                convertedIncompatibilities = convertedIncompatibilityBuilder.ToImmutable();
            }
        }

        return new ModManifest(
            guid,
            name,
            description,
            author,
            version,
            convertedAssets,
            convertedDependencies,
            convertedIncompatibilities);
    }

    /// <summary>
    /// Checks if two mod manifests are equal.
    /// </summary>
    /// <param name="left">
    /// The left hand side.
    /// </param>
    /// <param name="right">
    /// The right hand side.
    /// </param>
    /// <remarks>
    /// Mod manifests are equal if their
    /// <see cref="ModManifest.Guid">Guid</see>,
    /// <see cref="ModManifest.Name">Name</see>,
    /// <see cref="ModManifest.Author">Author</see>, and
    /// <see cref="ModManifest.Version">Version</see> are
    /// equal.
    /// <para>
    /// <b>
    /// <see cref="ModManifest.Description">Description</see>,
    /// <see cref="ModManifest.Dependencies">Dependencies</see>,
    /// and
    /// <see cref="ModManifest.Incompatibilities">Incompatibilities</see>
    /// are not checked.</b>
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/>
    /// equals <paramref name="right"/>;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool operator ==(ModManifest? left, ModManifest? right)
    {
        if (right is null) return left is null;
        else return right.Equals(left);
    }

    /// <summary>
    /// Checks if two mod manifests are not equal.
    /// </summary>
    /// <param name="left">
    /// The left hand side.
    /// </param>
    /// <param name="right">
    /// The right hand side.
    /// </param>
    /// <remarks>
    /// Mod manifests are not equal if their
    /// <see cref="ModManifest.Guid">Guid</see>,
    /// <see cref="ModManifest.Name">Name</see>,
    /// <see cref="ModManifest.Author">Author</see>, or
    /// <see cref="ModManifest.Version">Version</see> aren't
    /// equal.
    /// <para>
    /// <b>
    /// <see cref="ModManifest.Description">Description</see>,
    /// <see cref="ModManifest.Dependencies">Dependencies</see>,
    /// and
    /// <see cref="ModManifest.Incompatibilities">Incompatibilities</see>
    /// are not checked.</b>
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/>
    /// does not equals <paramref name="right"/>;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool operator !=(ModManifest? left, ModManifest? right)
    {
        if (right is null) return left is not null;
        else return !right.Equals(left);
    }
}