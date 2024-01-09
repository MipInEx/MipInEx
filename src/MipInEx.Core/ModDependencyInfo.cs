using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// Information about a mod dependency.
/// </summary>
public sealed class ModDependencyInfo : ModReferenceInfo, IEquatable<ModDependencyInfo>
{
    private readonly bool required;

    /// <summary>
    /// Initializes this mod dependency with the specified guid
    /// and whether or not it's required.
    /// <para>
    /// This will match a mod with the specified
    /// <paramref name="guid"/> regardless of the version of
    /// that mod.
    /// </para>
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod this reference is for.
    /// </param>
    /// <param name="required">
    /// Whether or not this dependency is required for the mod
    /// to function.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModDependencyInfo(string guid, bool required)
        : base(guid)
    {
        this.required = required;
    }

    /// <summary>
    /// Initializes this mod dependency with the specified
    /// guid, version, and whether or not it's required.
    /// <para>
    /// This will match a mod with the specified
    /// <paramref name="guid"/> and meets the version
    /// requirements of <paramref name="version"/>.
    /// </para>
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod this reference is for.
    /// </param>
    /// <param name="required">
    /// Whether or not this dependency is required for the mod
    /// to function.
    /// </param>
    /// <param name="version">
    /// The versions this reference is valid for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModDependencyInfo(string guid, bool required, IModReferenceVersionRequirement version)
        : base(guid, version)
    {
        this.required = required;
    }

    /// <summary>
    /// Initializes this mod dependency with the specified
    /// guid, versions, and whether or not it's required.
    /// <para>
    /// This will match a mod with the specified
    /// <paramref name="guid"/> and meets any version
    /// requirement in <paramref name="versions"/>.
    /// </para>
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod this reference is for.
    /// </param>
    /// <param name="required">
    /// Whether or not this dependency is required for the mod
    /// to function.
    /// </param>
    /// <param name="versions">
    /// The versions this reference is valid for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModDependencyInfo(string guid, bool required, params IModReferenceVersionRequirement[] versions)
        : base(guid, versions)
    {
        this.required = required;
    }

    /// <summary>
    /// Initializes this mod dependency with the specified
    /// guid, versions, and whether or not it's required.
    /// <para>
    /// This will match a mod with the specified
    /// <paramref name="guid"/> and meets any version
    /// requirement in <paramref name="versions"/>.
    /// </para>
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod this reference is for.
    /// </param>
    /// <param name="required">
    /// Whether or not this dependency is required for the mod
    /// to function.
    /// </param>
    /// <param name="versions">
    /// The versions this reference is valid for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModDependencyInfo(string guid, bool required, IEnumerable<IModReferenceVersionRequirement> versions)
        : base(guid, versions)
    {
        this.required = required;
    }

    internal ModDependencyInfo(string guid, bool required, ImmutableArray<IModReferenceVersionRequirement> versions)
        : base(guid, versions)
    {
        this.required = required;
    }

    /// <summary>
    /// Whether or not this dependency is required for the mod
    /// to function.
    /// </summary>
    public bool Required => this.required;

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] object? obj)
    {
        return this.Equals(obj as ModDependencyInfo);
    }

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] ModReferenceInfo? other)
    {
        return other is ModDependencyInfo dependencyInfo && this.Equals(dependencyInfo);
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] ModDependencyInfo? other)
    {
        return base.Equals(other) && this.required == other.required;
    }

    /// <inheritdoc/>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(this.Guid, this.Versions, this.required);
    }

    /// <inheritdoc/>
    public static bool operator ==(ModDependencyInfo? left, ModDependencyInfo? right)
    {
        if (right is null) return left is null;
        else return right.Equals(left);
    }

    /// <inheritdoc/>
    public static bool operator !=(ModDependencyInfo? left, ModDependencyInfo? right)
    {
        if (right is null) return left is not null;
        else return !right.Equals(left);
    }
}
