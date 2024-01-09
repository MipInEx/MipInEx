using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace MipInEx;

/// <summary>
/// A reference to a mod info.
/// </summary>
public class ModReferenceInfo : IEquatable<ModReferenceInfo>
{
    private readonly string guid;
    private readonly ImmutableArray<IModReferenceVersionRequirement> versions;

    /// <summary>
    /// Initializes this mod reference with the specified guid.
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
    public ModReferenceInfo(string guid)
    {
        if (guid is null) throw new ArgumentNullException(nameof(guid));

        this.guid = guid;
        this.versions = ImmutableArray<IModReferenceVersionRequirement>.Empty;
    }

    /// <summary>
    /// Initializes this mod reference with the specified guid
    /// and version.
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
    public ModReferenceInfo(string guid, IModReferenceVersionRequirement version)
    {
        if (guid is null) throw new ArgumentNullException(nameof(guid));

        this.guid = guid;
        this.versions = version is null ?
            ImmutableArray<IModReferenceVersionRequirement>.Empty :
            ImmutableArray.Create(version);
    }

    /// <summary>
    /// Initializes this mod reference with the specified guid
    /// and versions.
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
    public ModReferenceInfo(string guid, params IModReferenceVersionRequirement[] versions)
    {
        if (guid is null) throw new ArgumentNullException(nameof(guid));

        this.guid = guid;
        this.versions = versions is null ?
            ImmutableArray<IModReferenceVersionRequirement>.Empty :
            versions.Where(x => x is not null).ToImmutableArray();
    }

    /// <summary>
    /// Initializes this mod reference with the specified guid
    /// and versions.
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
    public ModReferenceInfo(string guid, IEnumerable<IModReferenceVersionRequirement> versions)
    {
        if (guid is null) throw new ArgumentNullException(nameof(guid));

        this.guid = guid;
        this.versions = versions is null ?
            ImmutableArray<IModReferenceVersionRequirement>.Empty :
            versions.Where(x => x is not null).ToImmutableArray();
    }

    private protected ModReferenceInfo(string guid, ImmutableArray<IModReferenceVersionRequirement> versions)
    {
        this.guid = guid;
        this.versions = versions;
    }

    /// <summary>
    /// The GUID of the mod this reference is for.
    /// </summary>
    public string Guid => this.guid;

    /// <summary>
    /// This list of versions this reference is valid for. If
    /// empty, is valid for any version.
    /// </summary>
    public IReadOnlyList<IModReferenceVersionRequirement> Versions => this.versions;

    /// <summary>
    /// Returns whether or not the specified mod matches this
    /// mod reference.
    /// </summary>
    /// <param name="mod">The mod to check.</param>
    /// <returns>
    /// <see langword="true"/> if the mod matches this
    /// reference; <see langword="false"/> otherwise.
    /// </returns>
    public bool Matches([NotNullWhen(true)] Mod? mod)
    {
        return this.Matches(mod?.Guid, mod?.Version);
    }

    /// <summary>
    /// Returns whether or not the specified mod matches this
    /// mod reference.
    /// </summary>
    /// <param name="mod">The mod to check.</param>
    /// <returns>
    /// <see langword="true"/> if the mod matches this
    /// reference; <see langword="false"/> otherwise.
    /// </returns>
    public bool Matches([NotNullWhen(true)] ModInfo? mod)
    {
        return this.Matches(mod?.Guid, mod?.Version);
    }

    /// <summary>
    /// Returns whether or not the specified guid and version
    /// match this mod reference.
    /// </summary>
    /// <param name="guid">The guid to check.</param>
    /// <param name="version">The version to check.</param>
    /// <returns>
    /// <see langword="true"/> if the guid and version match
    /// this reference; <see langword="false"/> otherwise.
    /// </returns>
    public bool Matches(
        [NotNullWhen(true)] string? guid,
        [NotNullWhen(true)] Version? version)
    {
        if (guid is null || version is null) return false;
        else if (this.guid != guid) return false;
        else return this.IncludesVersion(version);
    }

    /// <summary>
    /// Returns whether or not the specified mod version is
    /// included in this mod reference.
    /// </summary>
    /// <param name="version">The mod version to check.</param>
    /// <returns>
    /// <see langword="true"/> if the specified mod version is
    /// included in this mod reference; <see langword="false"/>
    /// otherwise.
    /// </returns>
    public bool IncludesVersion([NotNullWhen(true)] Version? version)
    {
        if (version is null) return false;
        else if (this.versions.Length == 0) return true;

        foreach (IModReferenceVersionRequirement versionRequirement in this.versions)
        {
            if (versionRequirement.MeetsRequirements(version))
            {
                return true;
            }
        }

        return false;
    }
    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return this.Equals(obj as ModReferenceInfo);
    }

    /// <inheritdoc/>
    public virtual bool Equals([NotNullWhen(true)] ModReferenceInfo? other)
    {
        if (other is null) return false;
        else if (object.ReferenceEquals(this, other)) return true;
        else if (this.guid != other.guid) return false;

        int count = this.versions.Length;
        if (other.versions.Length != count) return false;

        // todo: make checks between versions not require ordering
        for (int index = 0; index < count; index++)
        {
            if (!this.versions[index].Equals(other.versions[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.guid, this.versions);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (this.versions.Length == 0)
            return this.guid;

        return $"{this.guid} ({this.GetVersionString()})";
    }

    /// <summary>
    /// Gets the stringified version of the version
    /// requirements in this reference.
    /// </summary>
    /// <returns>
    /// A stringified version of the version requirements in
    /// this reference.
    /// </returns>
    public string GetVersionString()
    {
        if (this.versions.Length == 0)
        {
            return "Any Version";
        }

        StringBuilder builder = new();

        for (int index = 0; index < this.versions.Length; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(this.versions[index]);
        }

        return builder.ToString();
    }
}
