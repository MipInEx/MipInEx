using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// Information about a mod incompatibility.
/// </summary>
public sealed class ModIncompatibilityInfo : ModReferenceInfo
{
    /// <summary>
    /// Initializes this mod incompatibility with the specified
    /// guid.
    /// <para>
    /// This will match a mod with the specified
    /// <paramref name="guid"/> regardless of the version of
    /// that mod.
    /// </para>
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod this reference is for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModIncompatibilityInfo(string guid)
        : base(guid)
    { }

    /// <summary>
    /// Initializes this mod incompatibility with the specified
    /// guid and version.
    /// <para>
    /// This will match a mod with the specified
    /// <paramref name="guid"/> and meets the version
    /// requirements of <paramref name="version"/>.
    /// </para>
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod this reference is for.
    /// </param>
    /// <param name="version">
    /// The versions this reference is valid for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModIncompatibilityInfo(string guid, IModReferenceVersionRequirement version)
        : base(guid, version)
    { }

    /// <summary>
    /// Initializes this mod incompatibility with the specified
    /// guid and versions.
    /// <para>
    /// This will match a mod with the specified
    /// <paramref name="guid"/> and meets any version
    /// requirement in <paramref name="versions"/>.
    /// </para>
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod this reference is for.
    /// </param>
    /// <param name="versions">
    /// The versions this reference is valid for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModIncompatibilityInfo(string guid, params IModReferenceVersionRequirement[] versions)
        : base(guid, versions)
    { }

    /// <summary>
    /// Initializes this mod incompatibility with the specified
    /// guid and versions.
    /// <para>
    /// This will match a mod with the specified
    /// <paramref name="guid"/> and meets any version
    /// requirement in <paramref name="versions"/>.
    /// </para>
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod this reference is for.
    /// </param>
    /// <param name="versions">
    /// The versions this reference is valid for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModIncompatibilityInfo(string guid, IEnumerable<IModReferenceVersionRequirement> versions)
        : base(guid, versions)
    { }

    internal ModIncompatibilityInfo(string guid, ImmutableArray<IModReferenceVersionRequirement> versions)
        : base(guid, versions)
    { }

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] object? obj)
    {
        return this.Equals(obj as ModIncompatibilityInfo);
    }

    /// <inheritdoc/>
    public sealed override bool Equals([NotNullWhen(true)] ModReferenceInfo? other)
    {
        return other is ModIncompatibilityInfo && base.Equals(other);
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] ModIncompatibilityInfo? other)
    {
        return base.Equals(other);
    }

    /// <inheritdoc/>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(this.Guid, this.Versions);
    }

    /// <inheritdoc/>
    public static bool operator ==(ModIncompatibilityInfo? left, ModIncompatibilityInfo? right)
    {
        if (right is null) return left is null;
        else return right.Equals(left);
    }

    /// <inheritdoc/>
    public static bool operator !=(ModIncompatibilityInfo? left, ModIncompatibilityInfo? right)
    {
        if (right is null) return left is not null;
        else return !right.Equals(left);
    }
}
