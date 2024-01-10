using System;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// The metadata of a mod plugin.
/// </summary>
public abstract class ModPluginMetadata : IEquatable<ModPluginMetadata>
{
    private readonly string guid;
    private readonly string name;
    private readonly Version version;

    internal ModPluginMetadata(string guid, string name, Version version)
    {
        this.guid = guid;
        this.name = name;
        this.version = version;
    }

    /// <summary>
    /// The globally unique identifier of the mod plugin.
    /// </summary>
    public string Guid => this.guid;

    /// <summary>
    /// The name of the mod plugin.
    /// </summary>
    public string Name => this.name;

    /// <summary>
    /// The version of the mod plugin.
    /// </summary>
    public Version Version => this.version;

    /// <summary>
    /// The full globally unique identifier of the mod plugin.
    /// </summary>
    public abstract string FullGuid { get; }

    /// <summary>
    /// Whether or nor the metadata is for an internal mod
    /// plugin.
    /// </summary>
    public abstract bool IsInternal { get; }

    /// <summary>
    /// The root plugin metadata for this metadata. If this
    /// metadata is a root plugin metadata, then this will
    /// return <see langword="this"/>.
    /// </summary>
    public abstract ModRootPluginMetadata RootMetadata { get; }

    /// <summary>
    /// Gets an info string of this metadata.
    /// </summary>
    /// <returns>
    /// An info string of this metadata.
    /// </returns>
    public abstract string ToInfoString();

    /// <inheritdoc/>
    public abstract override int GetHashCode();

    /// <inheritdoc/>
    public abstract override bool Equals([NotNullWhen(true)] object? obj);

    /// <inheritdoc/>
    public abstract bool Equals([NotNullWhen(true)] ModPluginMetadata? other);
}
