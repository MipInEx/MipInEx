using System;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// The metadata of a root plugin.
/// </summary>
public sealed class ModRootPluginMetadata : ModPluginMetadata, IEquatable<ModRootPluginMetadata>
{
    internal ModRootPluginMetadata(string guid, string name, Version version)
        : base(guid, name, version)
    {
    }

    /// <inheritdoc cref="ModPluginMetadata.FullGuid"/>
    /// <remarks>
    /// Returns
    /// <c><see langword="this"/>.<see cref="ModPluginMetadata.Guid">Guid</see></c>
    /// </remarks>
    public sealed override string FullGuid => this.Guid;

    /// <inheritdoc cref="ModPluginMetadata.IsInternal"/>
    /// <remarks>
    /// Returns <c><see langword="false"/></c>
    /// </remarks>
    public sealed override bool IsInternal => false;

    /// <inheritdoc cref="ModPluginMetadata.RootMetadata"/>
    /// <remarks>
    /// Returns <c><see langword="this"/></c>
    /// </remarks>
    public sealed override ModRootPluginMetadata RootMetadata => this;

    /// <summary>
    /// Gets an info string of this root plugin metadata.
    /// </summary>
    /// <returns>
    /// <c>$"Plugin {<see langword="this"/>.<see cref="ModPluginMetadata.Name">Name</see>} v{<see langword="this"/>.<see cref="ModPluginMetadata.Version">Version</see>}"</c>
    /// </returns>
    public sealed override string ToInfoString()
    {
        return $"Plugin {this.Name} v{this.Version}";
    }

    /// <inheritdoc/>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(this.Guid, this.Name, this.Version);
    }

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] object? obj)
    {
        return this.Equals(obj as ModRootPluginMetadata);
    }

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] ModPluginMetadata? other)
    {
        return this.Equals(other as ModRootPluginMetadata);
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] ModRootPluginMetadata? other)
    {
        if (other is null)
            return false;
        else if (object.ReferenceEquals(this, other))
            return true;
        else
            return
                this.Guid == other.Guid &&
                this.Name == other.Name &&
                this.Version == other.Version;
    }

    /// <inheritdoc/>
    public static bool operator ==(ModRootPluginMetadata? left, ModRootPluginMetadata? right)
    {
        if (right is null) return left is null;
        else return right.Equals(left);
    }

    /// <inheritdoc/>
    public static bool operator !=(ModRootPluginMetadata? left, ModRootPluginMetadata? right)
    {
        if (right is null) return left is not null;
        else return !right.Equals(left);
    }
}
