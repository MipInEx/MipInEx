// Based on System.Version

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MipInEx;

/// <summary>
/// A representation of a version for a mod reference.
/// </summary>
/// <remarks>
/// This class is very similar to
/// <see cref="Version"/>, however instead of <c>-1</c>
/// denoting a component as unspecified, it instead means the
/// component is a wildcard. <see langword="null"/> denotes a
/// component as unspecified.
/// </remarks>
public sealed class ModReferenceVersion : 
    IFormattable,
    IComparable,
    IComparable<ModReferenceVersion>,
    IComparable<Version>,
    IEquatable<ModReferenceVersion>,
    IEquatable<Version>
{
    private readonly int major;
    private readonly int minor;
    private readonly int? build;
    private readonly int? revision;

    /// <summary>
    /// Initializes this mod dependency version with the
    /// following component values:
    /// <list type="table">
    /// <item>
    /// <term>
    /// <see cref="Major">major</see>
    /// </term>
    /// <description>
    /// <paramref name="major"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Minor">minor</see>
    /// </term>
    /// <description>
    /// <paramref name="minor"/> (will be the wildcard value if
    /// <see langword="null"/>)
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Build">build</see>
    /// </term>
    /// <description><see langword="null"/></description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Revision">revision</see>
    /// </term>
    /// <description><see langword="null"/></description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="major">
    /// The major version component.
    /// </param>
    /// <param name="minor">
    /// The minor version component.
    /// </param>
    public ModReferenceVersion(int major, int? minor)
    {
        this.major = major;
        this.minor = minor ?? -1;
        this.build = null;
        this.revision = null;
    }

    /// <summary>
    /// Initializes this mod dependency version with the
    /// following component values:
    /// <list type="table">
    /// <item>
    /// <term>
    /// <see cref="Major">major</see>
    /// </term>
    /// <description>
    /// <paramref name="major"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Minor">minor</see>
    /// </term>
    /// <description>
    /// <paramref name="minor"/> (will be the wildcard value if
    /// <see langword="null"/>)
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Build">build</see>
    /// </term>
    /// <description>
    /// <paramref name="build"/> (will be the wildcard value if
    /// <see langword="null"/>)
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Revision">revision</see>
    /// </term>
    /// <description><see langword="null"/></description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="major">
    /// The major version component.
    /// </param>
    /// <param name="minor">
    /// The minor version component.
    /// </param>
    /// <param name="build">
    /// The build version component.
    /// </param>
    public ModReferenceVersion(int major, int? minor, int? build)
    {
        this.major = major;
        this.minor = minor ?? -1;
        this.build = build ?? -1;
        this.revision = null;
    }

    /// <summary>
    /// Initializes this mod dependency version with the
    /// following component values:
    /// <list type="table">
    /// <item>
    /// <term>
    /// <see cref="Major">major</see>
    /// </term>
    /// <description>
    /// <paramref name="major"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Minor">minor</see>
    /// </term>
    /// <description>
    /// <paramref name="minor"/> (will be the wildcard value if
    /// <see langword="null"/>)
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Build">build</see>
    /// </term>
    /// <description>
    /// <paramref name="build"/> (will be the wildcard value if
    /// <see langword="null"/>)
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="Revision">revision</see>
    /// </term>
    /// <description>
    /// <paramref name="revision"/> (will be the wildcard value
    /// if <see langword="null"/>)
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="major">
    /// The major version component.
    /// </param>
    /// <param name="minor">
    /// The minor version component.
    /// </param>
    /// <param name="build">
    /// The build version component.
    /// </param>
    /// <param name="revision">
    /// The revision version component.
    /// </param>
    public ModReferenceVersion(int major, int? minor, int? build, int? revision)
    {
        this.major = major;
        this.minor = minor ?? -1;
        this.build = build ?? -1;
        this.revision = revision ?? -1;
    }

    private ModReferenceVersion(int major, int minor, int? build, int? revision)
    {
        this.major = major;
        this.minor = minor;
        this.build = build;
        this.revision = revision;
    }

    /// <summary>
    /// The major version component.
    /// </summary>
    public int Major => this.major;

    /// <summary>
    /// The minor version component.
    /// </summary>
    /// <remarks>
    /// If the value is less than <c>0</c>, then it is a
    /// <b>wildcard</b>.
    /// </remarks>
    public int Minor => this.minor;

    /// <summary>
    /// The build version component.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the value is <see langword="null"/>, then it is
    /// unspecified.
    /// </para>
    /// <para>
    /// If the value is less than <c>0</c>, then it is a
    /// <b>wildcard</b>.
    /// </para>
    /// </remarks>
    public int? Build => this.build;

    /// <summary>
    /// The revision version component.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the value is <see langword="null"/>, then it is
    /// unspecified.
    /// </para>
    /// <para>
    /// If the value is less than <c>0</c>, then it is a
    /// <b>wildcard</b>.
    /// </para>
    /// </remarks>
    public int? Revision => this.revision;

    /// <inheritdoc/>
    public int CompareTo(object? other)
    {
        if (other is null) return 1;
        else if (other is Version version) return this.CompareTo(version);
        else if (other is ModReferenceVersion referenceVersion) return this.CompareTo(referenceVersion);
        else throw new ArgumentException("Argument must be null, an instance of Version, or an instance of ModReferenceVersion", nameof(other));
    }

    /// <inheritdoc/>
    public int CompareTo(Version? other)
    {
        if (other is null) return 1;

        int result;

        result = this.major.CompareTo(other.Major);
        if (result != 0) return result;

        if (this.minor >= 0)
        {
            result = this.minor.CompareTo(other.Minor);
            if (result != 0) return result;
        }

        if (this.build.HasValue)
        {
            if (this.build.Value >= 0)
            {
                result = this.build.Value.CompareTo(other.Build);
                if (result != 0) return result;
            }
        }
        else if (other.Build >= 0)
        {
            return -1;
        }

        if (this.revision.HasValue)
        {
            if (this.revision.Value >= 0)
            {
                result = this.revision.Value.CompareTo(other.Revision);
                if (result != 0) return result;
            }
        }
        else if (other.Revision >= 0)
        {
            return -1;
        }

        return result;
    }

    /// <inheritdoc/>
    public int CompareTo(ModReferenceVersion? other)
    {
        if (object.ReferenceEquals(this, other)) return 0;
        else if (other is null) return 1;

        int result;

        result = this.major.CompareTo(other.major);
        if (result != 0) return result;

        if (this.minor >= 0 && other.minor >= 0)
        {
            result = this.minor.CompareTo(other.minor);
            if (result != 0) return result;
        }

        if (this.build.HasValue)
        {
            if (!other.build.HasValue) return 1;

            int thisBuild = this.build.Value;
            int otherBuild = other.build.Value;

            if (thisBuild >= 0 && otherBuild >= 0)
            {
                result = thisBuild.CompareTo(otherBuild);
                if (result != 0) return result;
            }
        }
        else if (other.build.HasValue)
        {
            return -1;
        }

        if (this.revision.HasValue)
        {
            if (!other.revision.HasValue) return 1;

            int thisRevision = this.revision.Value;
            int otherRevision = other.revision.Value;

            if (thisRevision >= 0 && otherRevision >= 0)
            {
                result = thisRevision.CompareTo(otherRevision);
                if (result != 0) return result;
            }
        }
        else if (other.revision.HasValue)
        {
            return -1;
        }

        return result;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.major, this.minor, this.build, this.revision);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is null) return false;
        else if (obj is Version version) return this.Equals(version);
        else return this.Equals(obj as ModReferenceVersion);
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] Version? other)
    {
        if (other is null) return false;
        else if (object.ReferenceEquals(other, this)) return true;

        if (this.major != other.Major) return false;

        if (this.minor >= 0 && this.minor != other.Minor)
        {
            return false;
        }

        if (this.build.HasValue)
        {
            if (this.build.Value >= 0 && this.build.Value != other.Build)
            {
                return false;
            }
        }
        else if (other.Build < 0)
        {
            return false;
        }

        if (this.revision.HasValue)
        {
            if (this.revision.Value >= 0 && this.revision.Value != other.Revision)
            {
                return false;
            }
        }
        else if (other.Revision < 0)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] ModReferenceVersion? other)
    {
        if (other is null) return false;
        else if (object.ReferenceEquals(other, this)) return true;

        if (this.major != other.major) return false;

        if (this.minor >= 0 && other.minor >= 0 && this.minor != other.minor)
        {
            return false;
        }

        if (this.build.HasValue)
        {
            if (!other.build.HasValue) return false;

            int thisBuild = this.build.Value;
            int otherBuild = other.build.Value;

            if (thisBuild >= 0 && otherBuild >= 0 && thisBuild != otherBuild)
            {
                return false;
            }
        }
        else if (other.build.HasValue)
        {
            return false;
        }

        if (this.revision.HasValue)
        {
            if (!other.revision.HasValue) return false;

            int thisRevision = this.revision.Value;
            int otherRevision = other.revision.Value;

            if (thisRevision >= 0 && otherRevision >= 0 && thisRevision != otherRevision)
            {
                return false;
            }
        }
        else if (other.revision.HasValue)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
        => this.ToString(this.DefaultFormatFieldCount);

    /// <inheritdoc/>
    public string ToString(int fieldCount)
    {
        Span<char> dest = stackalloc char[ModReferenceVersion.BufferSize];
        bool success = this.TryFormat(dest, fieldCount, out int charsWritten);
        Debug.Assert(success);
        return dest.Slice(0, charsWritten).ToString();
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        => this.ToString();

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten)
        => this.TryFormat(destination, this.DefaultFormatFieldCount, out charsWritten);

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, int fieldCount, out int charsWritten)
    {
        switch ((uint)fieldCount)
        {
            case > 4:
                throw new ArgumentOutOfRangeException("Field count must be >= 0 and <= 4.", nameof(fieldCount));
            case >= 3 when !this.build.HasValue:
                throw new ArgumentOutOfRangeException("Field count must be >= 0 and <= 2.", nameof(fieldCount));
            case 4 when !this.revision.HasValue:
                throw new ArgumentOutOfRangeException("Field count must be >= 0 and <= 3.", nameof(fieldCount));
        }

        int totalCharsWritten = 0;

        for (int i = 0; i < fieldCount; i++)
        {
            if (i != 0)
            {
                if (destination.IsEmpty)
                {
                    charsWritten = 0;
                    return false;
                }

                destination[0] = '.';
                destination = destination.Slice(i);
                totalCharsWritten++;
            }

            int value = i switch
            {
                0 => this.major,
                1 => this.minor,
                2 => this.build.GetValueOrDefault(0),
                _ => this.revision.GetValueOrDefault(0)
            };

            if (value < 0)
            {
                if (destination.IsEmpty)
                {
                    charsWritten = 0;
                    return false;
                }

                destination[0] = 'x';
                destination = destination.Slice(i);
                totalCharsWritten++;
            }

            bool formatted = value.TryFormat(destination, out int valueCharsWritten);

            if (!formatted)
            {
                charsWritten = 0;
                return false;
            }

            totalCharsWritten += valueCharsWritten;
            destination = destination.Slice(totalCharsWritten);
        }

        charsWritten = totalCharsWritten;
        return true;
    }

    private int DefaultFormatFieldCount
    {
        get
        {
            if (!this.build.HasValue) return 2;
            else if (!this.revision.HasValue) return 3;
            else return 4;
        }
    }


    /// <summary>
    /// Parses the given text input to a
    /// <see cref="ModReferenceVersion"/>. <c>x</c> and
    /// <c>X</c> specify a wildcard.
    /// </summary>
    /// <remarks>
    /// Valid formats (where <c>{COMPONENT}</c> is an integer):
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="input">
    /// The text input to parse.
    /// </param>
    /// <returns>
    /// The resulting dependency version.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="input"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> isn't a valid dependency
    /// version string.
    /// </exception>
    public static ModReferenceVersion Parse(string input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));

        return ModReferenceVersion.ParseVersion(input.AsSpan(), throwOnFailure: true)!;
    }

    /// <summary>
    /// Parses the given text input to a
    /// <see cref="ModReferenceVersion"/>. <c>x</c> and
    /// <c>X</c> specify a wildcard.
    /// </summary>
    /// <remarks>
    /// Valid formats (where <c>{COMPONENT}</c> is an integer):
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="input">
    /// The text input to parse.
    /// </param>
    /// <returns>
    /// The resulting dependency version.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> isn't a valid dependency
    /// version string.
    /// </exception>
    public static ModReferenceVersion Parse(ReadOnlySpan<char> input)
        => ModReferenceVersion.ParseVersion(input, throwOnFailure: true)!;

    /// <summary>
    /// Tries to parse the given text input to a
    /// <see cref="ModReferenceVersion"/>. <c>x</c> and
    /// <c>X</c> specify a wildcard.
    /// </summary>
    /// <remarks>
    /// Valid formats (where <c>{COMPONENT}</c> is an integer):
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="input">
    /// The text input to parse. If <see langword="null"/>,
    /// this method will return <see langword="false"/>.
    /// </param>
    /// <param name="result">
    /// The resulting dependency version. Will
    /// be <see langword="null"/> if this method returns
    /// <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if parsing was successful;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out ModReferenceVersion? result)
    {
        if (input is null)
        {
            result = null;
            return false;
        }

        result = ModReferenceVersion.ParseVersion(input.AsSpan(), throwOnFailure: false);
        return result is not null;
    }

    /// <summary>
    /// Tries to parse the given text input to a
    /// <see cref="ModReferenceVersion"/>. <c>x</c> and
    /// <c>X</c> specify a wildcard.
    /// </summary>
    /// <remarks>
    /// Valid formats (where <c>{COMPONENT}</c> is an integer):
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.x.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.x.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}.x</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>{COMPONENT}.{COMPONENT}.{COMPONENT}.{COMPONENT}</c>
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="input">
    /// The text input to parse.
    /// </param>
    /// <param name="result">
    /// The resulting dependency version. Will
    /// be <see langword="null"/> if this method returns
    /// <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if parsing was successful;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out ModReferenceVersion? result)
    {
        result = ModReferenceVersion.ParseVersion(input, throwOnFailure: false);
        return result is not null;
    }

    private static ModReferenceVersion? ParseVersion(ReadOnlySpan<char> input, bool throwOnFailure)
    {
        // Find the separator between major and minor.  It must exist.
        int majorEnd = input.IndexOf('.');

        if (majorEnd < 0)
        {
            if (throwOnFailure) throw new ArgumentException("Invalid version string", nameof(input));
            return null;
        }

        // Find the ends of the optional minor and build portions.
        // We musn't have any separators after build.

        int buildEnd = -1;
        int minorEnd = input.Slice(majorEnd + 1).IndexOf('.');

        if (minorEnd >= 0)
        {
            minorEnd += majorEnd + 1;
            buildEnd = input.Slice(minorEnd + 1).IndexOf('.');

            if (buildEnd >= 0)
            {
                buildEnd += minorEnd + 1;
                if (input.Slice(buildEnd + 1).IndexOf('.') > 0)
                {
                    if (throwOnFailure) throw new ArgumentException("Invalid version string", nameof(input));
                    return null;
                }
            }
        }

        int minor, build, revision;

        // Parse the major version
        if (!ModReferenceVersion.TryParseComponent(input.Slice(0, majorEnd), nameof(input), throwOnFailure, out int major))
        {
            return null;
        }

        if (minorEnd != -1)
        {
            // If there's more than a major and minor, parse the minor, too.
            if (!ModReferenceVersion.TryParseComponentWithWildcard(input.Slice(majorEnd + 1, minorEnd - majorEnd - 1), nameof(input), throwOnFailure, out minor))
            {
                return null;
            }

            if (buildEnd != -1)
            {
                // major.minor.build.revision
                if (ModReferenceVersion.TryParseComponentWithWildcard(input.Slice(minorEnd + 1, buildEnd - minorEnd - 1), nameof(build), throwOnFailure, out build) &&
                    ModReferenceVersion.TryParseComponentWithWildcard(input.Slice(buildEnd + 1), nameof(revision), throwOnFailure, out revision))
                {
                    return new ModReferenceVersion(major, minor, build, revision);
                }

                return null;
            }
            // major.minor.build
            else if (ModReferenceVersion.TryParseComponentWithWildcard(input.Slice(minorEnd + 1), nameof(build), throwOnFailure, out build))
            {
                return new ModReferenceVersion(major, minor, build, null);
            }
            else
            {
                return null;
            }
        }
        // major.minor
        else if (ModReferenceVersion.TryParseComponentWithWildcard(input.Slice(majorEnd + 1), nameof(input), throwOnFailure, out minor))
        {
            return new ModReferenceVersion(major, minor, null, null);
        }
        else
        {
            return null;
        }
    }

    private static bool TryParseComponent(ReadOnlySpan<char> component, string componentName, bool throwOnFailure, out int parsedComponent)
    {
        if (throwOnFailure)
        {
            parsedComponent = int.Parse(component, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (parsedComponent < 0) throw new ArgumentOutOfRangeException(componentName, $"Cannot be negative. (Was {parsedComponent})");
            return true;
        }

        return int.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedComponent) && parsedComponent >= 0;
    }

    private static bool TryParseComponentWithWildcard(ReadOnlySpan<char> component, string componentName, bool throwOnFailure, out int parsedComponent)
    {
        if (!component.IsEmpty && component.Length == 1 &&
            (component[0] == 'x' || component[0] == 'X'))
        {
            parsedComponent = -1;
            return true;
        }

        return ModReferenceVersion.TryParseComponent(component, componentName, throwOnFailure, out parsedComponent);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ModReferenceVersion? left, ModReferenceVersion? right)
    {
        // Test "right" first to allow branch elimination when inlined for null checks (== null)
        // so it can become a simple test
        if (right is null)
        {
            return left is null;
        }

        // Quick reference equality test prior to calling the virtual Equality
        if (object.ReferenceEquals(right, left)) return true;
        return right.Equals(left);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ModReferenceVersion? left, ModReferenceVersion? right)
    {
        // Test "right" first to allow branch elimination when inlined for null checks (== null)
        // so it can become a simple test
        if (right is null)
        {
            return left is not null;
        }

        // Quick reference equality test prior to calling the virtual Equality
        if (object.ReferenceEquals(right, left)) return false;
        return !right.Equals(left);
    }

    /// <inheritdoc/>
    public static bool operator <(ModReferenceVersion? left, ModReferenceVersion? right)
    {
        if (left is null) return right is not null;
        else return left.CompareTo(right) < 0;
    }

    /// <inheritdoc/>
    public static bool operator <=(ModReferenceVersion? left, ModReferenceVersion? right)
    {
        if (left is null) return true;
        else return left.CompareTo(right) <= 0;
    }

    /// <inheritdoc/>
    public static bool operator >(ModReferenceVersion? left, ModReferenceVersion? right)
    {
        if (left is null) return right is not null;
        else return left.CompareTo(right) > 0;
    }

    /// <inheritdoc/>
    public static bool operator >=(ModReferenceVersion? left, ModReferenceVersion? right)
    {
        if (left is null) return right is null;
        else return left.CompareTo(right) >= 0;
    }

    // int32s can be 11 chars in length
    // max: 4 int32s + 3 periods
    internal const int BufferSize = (4 * 11) + 3;
}
