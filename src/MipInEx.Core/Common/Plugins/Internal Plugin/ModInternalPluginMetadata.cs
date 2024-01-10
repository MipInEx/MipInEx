using System;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// The metadata of an internal plugin.
/// </summary>
public sealed class ModInternalPluginMetadata : ModPluginMetadata, IEquatable<ModInternalPluginMetadata>
{
    private readonly ModRootPluginMetadata rootMetadata;

    internal ModInternalPluginMetadata(ModRootPluginMetadata rootMetadata, string guid, string name, Version version)
        : base(guid, name, version)
    {
        this.rootMetadata = rootMetadata;
    }

    /// <inheritdoc cref="ModPluginMetadata.FullGuid"/>
    /// <remarks>
    /// Returns
    /// <c>$"{<see langword="this"/>.<see cref="ModInternalPluginMetadata.RootMetadata">RootMetadata</see>.<see cref="ModPluginMetadata.Guid">Guid</see>}~{<see langword="this"/>.<see cref="ModPluginMetadata.Guid">Guid</see>}"</c>
    /// </remarks>
    public sealed override string FullGuid => this.rootMetadata.Guid + '~' + this.Guid;

    /// <inheritdoc cref="ModPluginMetadata.IsInternal"/>
    /// <remarks>
    /// Returns <c><see langword="true"/></c>
    /// </remarks>
    public sealed override bool IsInternal => true;

    /// <inheritdoc cref="ModPluginMetadata.RootMetadata"/>
    public sealed override ModRootPluginMetadata RootMetadata => this.rootMetadata;

    /// <summary>
    /// Gets an info string of this internal plugin metadata.
    /// </summary>
    /// <returns>
    /// <c>$"Internal Plugin ({<see langword="this"/>.<see cref="ModInternalPluginMetadata.RootMetadata">RootMetadata</see>.<see cref="ModPluginMetadata.Name">Name</see>}) {<see langword="this"/>.<see cref="ModPluginMetadata.Name">Name</see>} v{<see langword="this"/>.<see cref="ModPluginMetadata.Version">Version</see>}"</c>
    /// </returns>
    public sealed override string ToInfoString()
    {
        return $"Internal Plugin ({this.rootMetadata.Name}) {this.Name} v{this.Version}";
    }

    /// <inheritdoc/>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(this.Guid, this.Name, this.Version, this.RootMetadata);
    }

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] object? obj)
    {
        return this.Equals(obj as ModInternalPluginMetadata);
    }

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] ModPluginMetadata? other)
    {
        return this.Equals(other as ModInternalPluginMetadata);
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] ModInternalPluginMetadata? other)
    {
        if (other is null)
            return false;
        else if (object.ReferenceEquals(this, other))
            return true;
        else
            return
                this.Guid == other.Guid &&
                this.Name == other.Name &&
                this.Version == other.Version &&
                this.RootMetadata.Equals(other.RootMetadata);
    }

    /// <inheritdoc/>
    public static bool operator ==(ModInternalPluginMetadata? left, ModInternalPluginMetadata? right)
    {
        if (right is null) return left is null;
        else return right.Equals(left);
    }

    /// <inheritdoc/>
    public static bool operator !=(ModInternalPluginMetadata? left, ModInternalPluginMetadata? right)
    {
        if (right is null) return left is not null;
        else return !right.Equals(left);
    }
}
