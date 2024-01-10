using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx
{
    /// <summary>
    /// A representation of a mod reference version
    /// requirement.
    /// </summary>
    /// <remarks>
    /// Can take any of the following representations (where
    /// <c>{VERSION}</c> is a
    /// <see cref="ModReferenceVersion"/>)
    /// <list type="bullet">
    /// <item>
    /// <term>
    /// <see cref="ModReferenceVersionRequirement.Exact">Exact</see>
    /// </term>
    /// <description><c>{VERSION}</c></description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="ModReferenceVersionRequirement.LessThan">LessThan</see>
    /// </term>
    /// <description>
    /// <c>&lt;{VERSION}</c> <i>or</i> <c>&lt;={VERSION}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="ModReferenceVersionRequirement.GreaterThan">GreaterThan</see>
    /// </term>
    /// <description>
    /// <c>&gt;{VERSION}</c> <i>or</i> <c>&gt;={VERSION}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// <see cref="ModReferenceVersionRequirement.Range">Range</see>
    /// </term>
    /// <description>
    /// <c>{VERSION}&lt;n&lt;{VERSION}</c> <i>or</i>
    /// <c>{VERSION}&lt;=n&lt;{VERSION}</c> <i>or</i>
    /// <c>{VERSION}&lt;n&lt;={VERSION}</c> <i>or</i>
    /// <c>{VERSION}&lt;=n&lt;={VERSION}</c>
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public interface IModReferenceVersionRequirement : IEquatable<IModReferenceVersionRequirement>
    {
        /// <summary>
        /// The type of requirement.
        /// </summary>
        ModReferenceVersionRequirement.Type Type { get; }

        /// <summary>
        /// Checks whether the specified version meets the
        /// version requirements.
        /// </summary>
        /// <param name="version">
        /// The version to test.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the version meets the
        /// requirements; <see langword="false"/> otherwise.
        /// </returns>
        bool MeetsRequirements([NotNullWhen(true)] Version? version);
    }

    /// <summary>
    /// A class containing implementation details for
    /// <see cref="IModReferenceVersionRequirement"/>.
    /// </summary>
    public static class ModReferenceVersionRequirement
    {
        /// <summary>
        /// The type of
        /// <see cref="IModReferenceVersionRequirement"/>.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// The version must be exact.
            /// </summary>
            /// <remarks>
            /// See
            /// <see cref="ModReferenceVersionRequirement.Exact"/>.
            /// </remarks>
            Exact,

            /// <summary>
            /// The version must be less than a given version.
            /// </summary>
            /// <remarks>
            /// See
            /// <see cref="ModReferenceVersionRequirement.LessThan"/>.
            /// </remarks>
            LessThan,

            /// <summary>
            /// The version must be greater than a given version.
            /// </summary>
            /// <remarks>
            /// See
            /// <see cref="ModReferenceVersionRequirement.GreaterThan"/>.
            /// </remarks>
            GreaterThan,


            /// <summary>
            /// The version must be in the range of two given
            /// versions.
            /// </summary>
            /// <remarks>
            /// See
            /// <see cref="ModReferenceVersionRequirement.Range"/>.
            /// </remarks>
            Range
        }

        private static readonly string ParseExceptionMessageBase = "Invalid requirement version string ";
        private static readonly string ParseExceptionMessageUOI = ModReferenceVersionRequirement.ParseExceptionMessageBase + "Unexpected end of input.";
        private static readonly string ParseExceptionMessageVer = ModReferenceVersionRequirement.ParseExceptionMessageBase + "Invalid version string.";
        private static readonly string ParseExceptionMessagePlaceholder = ModReferenceVersionRequirement.ParseExceptionMessageBase + "'n' placeholder not found.";
        private static readonly string ParseExceptionMessagePlaceholderAfter = ModReferenceVersionRequirement.ParseExceptionMessageBase + "Expected '<' after 'n' placeholder.";

        /// <summary>
        /// Parses the given text input to a
        /// <see cref="IModReferenceVersionRequirement"/>,
        /// which will be either <see cref="Exact"/>,
        /// <see cref="LessThan"/>,
        /// <see cref="GreaterThan"/>, or
        /// <see cref="Range"/>.
        /// </summary>
        /// <remarks>
        /// Valid formats (where
        /// <c>{VERSION}</c> is a
        /// <see cref="ModReferenceVersion"/>):
        /// <list type="bullet">
        /// <item>
        /// <description><c>{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&lt;{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&lt;={VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&gt;{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&gt;={VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;n&lt;{VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;=n&lt;{VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;n&lt;={VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;=n&lt;={VERSION}</c>
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="input">
        /// The text input to parse.
        /// </param>
        /// <returns>
        /// The resulting reference version requirement.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="input"/> isn't a valid reference
        /// version requirement string.
        /// </exception>
        public static IModReferenceVersionRequirement Parse(string? input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            return ModReferenceVersionRequirement.ParseRequirement(input.AsSpan(), throwOnFailure: true)!;
        }


        /// <summary>
        /// Parses the given text input to a
        /// <see cref="IModReferenceVersionRequirement"/>,
        /// which will be either <see cref="Exact"/>,
        /// <see cref="LessThan"/>,
        /// <see cref="GreaterThan"/>, or
        /// <see cref="Range"/>.
        /// </summary>
        /// <remarks>
        /// Valid formats (where
        /// <c>{VERSION}</c> is a
        /// <see cref="ModReferenceVersion"/>):
        /// <list type="bullet">
        /// <item>
        /// <description><c>{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&lt;{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&lt;={VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&gt;{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&gt;={VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;n&lt;{VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;=n&lt;{VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;n&lt;={VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;=n&lt;={VERSION}</c>
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="input">
        /// The text input to parse.
        /// </param>
        /// <returns>
        /// The resulting reference version requirement.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="input"/> isn't a valid reference
        /// version requirement string.
        /// </exception>
        public static IModReferenceVersionRequirement Parse(ReadOnlySpan<char> input)
            => ModReferenceVersionRequirement.ParseRequirement(input, throwOnFailure: true)!;

        /// <summary>
        /// Tries to parse the given text input to a
        /// <see cref="IModReferenceVersionRequirement"/>,
        /// which will be either <see cref="Exact"/>,
        /// <see cref="LessThan"/>,
        /// <see cref="GreaterThan"/>, or
        /// <see cref="Range"/>.
        /// </summary>
        /// <remarks>
        /// Valid formats (where
        /// <c>{VERSION}</c> is a
        /// <see cref="ModReferenceVersion"/>):
        /// <list type="bullet">
        /// <item>
        /// <description><c>{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&lt;{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&lt;={VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&gt;{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&gt;={VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;n&lt;{VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;=n&lt;{VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;n&lt;={VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;=n&lt;={VERSION}</c>
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="input">
        /// The text input to parse. If <see langword="null"/>,
        /// this method will return <see langword="false"/>.
        /// </param>
        /// <param name="result">
        /// The resulting reference version requirement. Will
        /// be <see langword="null"/> if this method returns
        /// <see langword="false"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if parsing was successful;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out IModReferenceVersionRequirement? result)
        {
            if (input is null)
            {
                result = null;
                return false;
            }

            result = ModReferenceVersionRequirement.ParseRequirement(input.AsSpan(), throwOnFailure: false);
            return result is not null;
        }


        /// <summary>
        /// Tries to parse the given text input to a
        /// <see cref="IModReferenceVersionRequirement"/>,
        /// which will be either <see cref="Exact"/>,
        /// <see cref="LessThan"/>,
        /// <see cref="GreaterThan"/>, or
        /// <see cref="Range"/>.
        /// </summary>
        /// <remarks>
        /// Valid formats (where
        /// <c>{VERSION}</c> is a
        /// <see cref="ModReferenceVersion"/>):
        /// <list type="bullet">
        /// <item>
        /// <description><c>{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&lt;{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&lt;={VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&gt;{VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description><c>&gt;={VERSION}</c></description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;n&lt;{VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;=n&lt;{VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;n&lt;={VERSION}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>{VERSION}&lt;=n&lt;={VERSION}</c>
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="input">
        /// The text input to parse.
        /// </param>
        /// <param name="result">
        /// The resulting reference version requirement. Will
        /// be <see langword="null"/> if this method returns
        /// <see langword="false"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if parsing was successful;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out IModReferenceVersionRequirement? result)
        {
            result = ModReferenceVersionRequirement.ParseRequirement(input, throwOnFailure: false);
            return result is not null;
        }

        private static IModReferenceVersionRequirement? ParseRequirement(ReadOnlySpan<char> input, bool throwOnFailure)
        {
            if (input.IsEmpty)
            {
                if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageUOI, nameof(input));
                return null;
            }

            ModReferenceVersion? version;
            bool exclusive;

            switch (input[0])
            {
                case '<':
                    input = input.Slice(1);
                    if (input.IsEmpty)
                    {
                        if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageUOI, nameof(input));
                        return null;
                    }

                    if (input[0] == '=')
                    {
                        exclusive = false;
                        input = input.Slice(1);
                    }
                    else
                    {
                        exclusive = true;
                    }

                    if (!ModReferenceVersion.TryParse(input, out version))
                    {
                        if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageVer, nameof(input));
                        return null;
                    }

                    return new LessThan(version, exclusive);
                case '>':
                    input = input.Slice(1);
                    if (input.IsEmpty)
                    {
                        if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageUOI, nameof(input));
                        return null;
                    }

                    if (input[0] == '=')
                    {
                        exclusive = false;
                        input = input.Slice(1);
                    }
                    else
                    {
                        exclusive = true;
                    }

                    if (!ModReferenceVersion.TryParse(input, out version))
                    {
                        if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageVer, nameof(input));
                        return null;
                    }

                    return new GreaterThan(version, exclusive);
            }

            int separatorIndex = input.IndexOf('<');
            if (separatorIndex < 0)
            {
                if (ModReferenceVersion.TryParse(input, out version))
                {
                    return new Exact(version);
                }

                if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageVer, nameof(input));
                return null;
            }

            if (!ModReferenceVersion.TryParse(input.Slice(0, separatorIndex), out version))
            {
                if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageVer, nameof(input));
                return null;
            }

            input = input.Slice(separatorIndex);
            if (input.Length <= 4)
            {
                if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageUOI, nameof(input));
                return null;
            }

            if (input[0] == '=')
            {
                exclusive = false;
                input = input.Slice(1);
            }
            else
            {
                exclusive = true;
            }

            if (input[0] != 'n')
            {
                if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessagePlaceholder, nameof(input));
                return null;
            }

            input = input.Slice(1);
            if (input[0] != '<')
            {
                if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessagePlaceholderAfter, nameof(input));
                return null;
            }
            input = input.Slice(1);

            bool maxExclusive;
            if (input[0] == '=')
            {
                maxExclusive = false;
                input = input.Slice(1);
            }
            else
            {
                maxExclusive = true;
            }

            if (!ModReferenceVersion.TryParse(input, out ModReferenceVersion? maxVersion))
            {
                if (throwOnFailure) throw new ArgumentException(ModReferenceVersionRequirement.ParseExceptionMessageVer, nameof(input));
                return null;
            }

            return new Range(version, maxVersion, exclusive, maxExclusive);
        }

        /// <summary>
        /// Creates an exact version requirement.
        /// </summary>
        /// <param name="version">
        /// The exact version.
        /// </param>
        /// <returns>
        /// An <see cref="Exact"/> version requirement.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="version"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static Exact CreateExact(ModReferenceVersion version)
        {
            if (version is null) throw new ArgumentNullException(nameof(version));

            return new Exact(version);
        }

        /// <summary>
        /// Creates a maximum version requirement.
        /// </summary>
        /// <param name="maxVersion">
        /// The maximum version.
        /// </param>
        /// <param name="exclusive">
        /// Whether or not <paramref name="maxVersion"/> also
        /// meets the requirement.
        /// </param>
        /// <returns>
        /// An <see cref="LessThan"/> version requirement.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="maxVersion"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static LessThan CreateLessThan(ModReferenceVersion maxVersion, bool exclusive)
        {
            if (maxVersion is null) throw new ArgumentNullException(nameof(maxVersion));

            return new LessThan(maxVersion, exclusive);
        }

        /// <summary>
        /// Creates a minimum version requirement.
        /// </summary>
        /// <param name="minVersion">
        /// The minimum version.
        /// </param>
        /// <param name="exclusive">
        /// Whether or not <paramref name="minVersion"/> also
        /// meets the requirement.
        /// </param>
        /// <returns>
        /// An <see cref="GreaterThan"/> version requirement.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="minVersion"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static GreaterThan CreateGreaterThan(ModReferenceVersion minVersion, bool exclusive)
        {
            if (minVersion is null) throw new ArgumentNullException(nameof(minVersion));

            return new GreaterThan(minVersion, exclusive);
        }

        /// <summary>
        /// Creates a requirement for a range of versions.
        /// </summary>
        /// <param name="minVersion">
        /// The minimum version.
        /// </param>
        /// <param name="maxVersion">
        /// The maximum version.
        /// </param>
        /// <param name="minVersionExclusive">
        /// Whether or not <paramref name="minVersion"/> also
        /// meets the requirement.
        /// </param>
        /// <param name="maxVersionExclusive">
        /// Whether or not <paramref name="maxVersion"/> also
        /// meets the requirement.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="minVersion"/> or
        /// <paramref name="maxVersion"/> are
        /// <see langword="null"/>.
        /// </exception>
        public static Range CreateRange(ModReferenceVersion minVersion, ModReferenceVersion maxVersion, bool minVersionExclusive, bool maxVersionExclusive)
        {
            if (minVersion is null) throw new ArgumentNullException(nameof(minVersion));
            if (maxVersion is null) throw new ArgumentNullException(nameof(maxVersion));

            return new Range(minVersion, maxVersion, minVersionExclusive, maxVersionExclusive);
        }

        /// <summary>
        /// Represents an exact version requirement.
        /// </summary>
        public sealed class Exact : IModReferenceVersionRequirement, IEquatable<Exact>
        {
            private readonly ModReferenceVersion version;

            internal Exact(ModReferenceVersion version)
            {
                this.version = version;
            }

            /// <inheritdoc/>
            public Type Type => Type.Exact;

            /// <summary>
            /// The exact version this requirement matches.
            /// </summary>
            public ModReferenceVersion Version => this.version;

            /// <inheritdoc/>
            public bool MeetsRequirements([NotNullWhen(true)] Version? version)
            {
                return version is not null && this.version.Equals(version);
            }

            /// <inheritdoc/>
            public sealed override bool Equals([NotNullWhen(true)] object? obj)
            {
                return this.Equals(obj as Exact);
            }

            /// <inheritdoc/>
            public bool Equals([NotNullWhen(true)] IModReferenceVersionRequirement? other)
            {
                return this.Equals(other as Exact);
            }

            /// <inheritdoc/>
            public bool Equals([NotNullWhen(true)] Exact? other)
            {
                return other is not null &&
                    this.version.Equals(other.version);
            }

            /// <inheritdoc/>
            public sealed override int GetHashCode()
            {
                return HashCode.Combine(this.Type, this.version);
            }

            /// <inheritdoc/>
            public sealed override string ToString()
            {
                Span<char> dest = stackalloc char[Exact.BufferSize];
                bool success = this.TryFormat(dest, out int charsWritten);
                Debug.Assert(success);
                return dest.Slice(0, charsWritten).ToString();
            }

            /// <inheritdoc/>
            public bool TryFormat(Span<char> destination, out int charsWritten)
            {
                return this.version.TryFormat(destination, out charsWritten);
            }

            private const int BufferSize = ModReferenceVersion.BufferSize;
        }

        /// <summary>
        /// Represents a maximum (less than) version
        /// requirement.
        /// </summary>
        public sealed class LessThan : IModReferenceVersionRequirement, IEquatable<LessThan>
        {
            private readonly ModReferenceVersion maxVersion;
            private readonly bool exclusive;

            internal LessThan(ModReferenceVersion maxVersion, bool exclusive)
            {
                this.maxVersion = maxVersion;
                this.exclusive = exclusive;
            }

            /// <inheritdoc/>
            public Type Type => Type.LessThan;

            /// <summary>
            /// The maximum version this requirement allows.
            /// </summary>
            public ModReferenceVersion MaxVersion => this.maxVersion;

            /// <summary>
            /// Whether or not <see cref="MaxVersion"/> meets
            /// the requirement.
            /// </summary>
            public bool Exclusive => this.exclusive;

            /// <inheritdoc/>
            public bool MeetsRequirements([NotNullWhen(true)] Version? version)
            {
                if (version is null) return false;
                else if (this.exclusive) return this.maxVersion.CompareTo(version) < 0;
                else return this.maxVersion.CompareTo(version) <= 0;
            }

            /// <inheritdoc/>
            public sealed override bool Equals([NotNullWhen(true)] object? obj)
            {
                return this.Equals(obj as LessThan);
            }

            /// <inheritdoc/>
            public bool Equals([NotNullWhen(true)] IModReferenceVersionRequirement? other)
            {
                return this.Equals(other as LessThan);
            }

            /// <inheritdoc/>
            public bool Equals([NotNullWhen(true)] LessThan? other)
            {
                return other is not null &&
                    this.exclusive == other.exclusive &&
                    this.maxVersion.Equals(other.maxVersion);
            }

            /// <inheritdoc/>
            public sealed override int GetHashCode()
            {
                return HashCode.Combine(this.Type, this.maxVersion, this.exclusive);
            }

            /// <inheritdoc/>
            public sealed override string ToString()
            {
                Span<char> dest = stackalloc char[LessThan.BufferSize];
                bool success = this.TryFormat(dest, out int charsWritten);
                Debug.Assert(success);
                return dest.Slice(0, charsWritten).ToString();
            }

            /// <inheritdoc/>
            public bool TryFormat(Span<char> destination, out int charsWritten)
            {
                int totalCharsWritten = 0;

                if (destination.IsEmpty)
                {
                    charsWritten = 0;
                    return false;
                }

                destination[0] = '<';
                destination = destination.Slice(1);
                totalCharsWritten++;

                if (!this.exclusive)
                {
                    if (destination.IsEmpty)
                    {
                        charsWritten = 0;
                        return false;
                    }
                    destination[0] = '=';
                    destination = destination.Slice(1);
                    totalCharsWritten++;
                }

                if (this.maxVersion.TryFormat(destination, out int valueCharsWritten))
                {
                    charsWritten = valueCharsWritten + totalCharsWritten;
                    return true;
                }
                else
                {
                    charsWritten = 0;
                    return false;
                }
            }

            // <= - 2 characters
            private const int BufferSize = ModReferenceVersion.BufferSize + 2;
        }

        /// <summary>
        /// Represents a minimum (greater than) version
        /// requirement.
        /// </summary>
        public sealed class GreaterThan : IModReferenceVersionRequirement, IEquatable<GreaterThan>
        {
            private readonly ModReferenceVersion minVersion;
            private readonly bool exclusive;

            internal GreaterThan(ModReferenceVersion minVersion, bool exclusive)
            {
                this.minVersion = minVersion;
                this.exclusive = exclusive;
            }

            /// <inheritdoc/>
            public Type Type => Type.GreaterThan;

            /// <summary>
            /// The minimum version this requirement allows.
            /// </summary>
            public ModReferenceVersion MinVersion => this.minVersion;

            /// <summary>
            /// Whether or not <see cref="MinVersion"/> meets
            /// the requirement.
            /// </summary>
            public bool Exclusive => this.exclusive;

            /// <inheritdoc/>
            public bool MeetsRequirements([NotNullWhen(true)] Version? version)
            {
                if (version is null) return false;
                else if (this.exclusive) return this.minVersion.CompareTo(version) > 0;
                else return this.minVersion.CompareTo(version) >= 0;
            }

            /// <inheritdoc/>
            public sealed override bool Equals([NotNullWhen(true)] object? obj)
            {
                return this.Equals(obj as GreaterThan);
            }

            /// <inheritdoc/>
            public bool Equals([NotNullWhen(true)] IModReferenceVersionRequirement? other)
            {
                return this.Equals(other as GreaterThan);
            }

            /// <inheritdoc/>
            public bool Equals([NotNullWhen(true)] GreaterThan? other)
            {
                return other is not null &&
                    this.exclusive == other.exclusive &&
                    this.minVersion.Equals(other.minVersion);
            }

            /// <inheritdoc/>
            public sealed override int GetHashCode()
            {
                return HashCode.Combine(this.Type, this.minVersion, this.exclusive);
            }

            /// <inheritdoc/>
            public sealed override string ToString()
            {
                Span<char> dest = stackalloc char[GreaterThan.BufferSize];
                bool success = this.TryFormat(dest, out int charsWritten);
                Debug.Assert(success);
                return dest.Slice(0, charsWritten).ToString();
            }

            /// <inheritdoc/>
            public bool TryFormat(Span<char> destination, out int charsWritten)
            {
                int totalCharsWritten = 0;

                if (destination.IsEmpty)
                {
                    charsWritten = 0;
                    return false;
                }

                destination[0] = '>';
                destination = destination.Slice(1);
                totalCharsWritten++;

                if (!this.exclusive)
                {
                    if (destination.IsEmpty)
                    {
                        charsWritten = 0;
                        return false;
                    }
                    destination[0] = '=';
                    destination = destination.Slice(1);
                    totalCharsWritten++;
                }

                if (this.minVersion.TryFormat(destination, out int valueCharsWritten))
                {
                    charsWritten = valueCharsWritten + totalCharsWritten;
                    return true;
                }
                else
                {
                    charsWritten = 0;
                    return false;
                }
            }

            // >= - 2 characters
            private const int BufferSize = ModReferenceVersion.BufferSize + 2;
        }

        /// <summary>
        /// Represents a range a valid versions.
        /// </summary>
        public sealed class Range : IModReferenceVersionRequirement
        {
            private readonly ModReferenceVersion minVersion;
            private readonly ModReferenceVersion maxVersion;
            private readonly bool minVersionExclusive;
            private readonly bool maxVersionExclusive;

            internal Range(ModReferenceVersion minVersion, ModReferenceVersion maxVersion, bool minVersionExclusive, bool maxVersionExclusive)
            {
                this.minVersion = minVersion;
                this.maxVersion = maxVersion;
                this.minVersionExclusive = minVersionExclusive;
                this.maxVersionExclusive = maxVersionExclusive;
            }

            /// <inheritdoc/>
            public Type Type => Type.Range;

            /// <summary>
            /// The minimum version this requirement allows.
            /// </summary>
            public ModReferenceVersion MinVersion => this.minVersion;

            /// <summary>
            /// The maximum version this requirement allows.
            /// </summary>
            public ModReferenceVersion MaxVersion => this.maxVersion;

            /// <summary>
            /// Whether or not <see cref="MinVersion"/> meets
            /// the requirement.
            /// </summary>
            public bool MinVersionExclusive => this.minVersionExclusive;

            /// <summary>
            /// Whether or not <see cref="MaxVersion"/> meets
            /// the requirement.
            /// </summary>
            public bool MaxVersionExclusive => this.maxVersionExclusive;

            /// <inheritdoc/>
            public bool MeetsRequirements([NotNullWhen(true)] Version? version)
            {
                if (version is null) return false;

                if (this.minVersionExclusive)
                {
                    if (this.minVersion.CompareTo(version) <= 0) return false;
                }
                else if (this.minVersion.CompareTo(version) < 0)
                {
                     return false;
                }

                if (this.maxVersionExclusive)
                {
                    if (this.maxVersion.CompareTo(version) >= 0) return false;
                }
                else if (this.maxVersion.CompareTo(version) > 0)
                {
                    return false;
                }

                return true;
            }

            /// <inheritdoc/>
            public sealed override bool Equals([NotNullWhen(true)] object? obj)
            {
                return this.Equals(obj as Range);
            }

            /// <inheritdoc/>
            public bool Equals([NotNullWhen(true)] IModReferenceVersionRequirement? other)
            {
                return this.Equals(other as Range);
            }

            /// <inheritdoc/>
            public bool Equals([NotNullWhen(true)] Range? other)
            {
                return other is not null &&
                    this.minVersionExclusive == other.minVersionExclusive &&
                    this.maxVersionExclusive == other.maxVersionExclusive &&
                    this.minVersion.Equals(other.minVersion) &&
                    this.maxVersion.Equals(other.maxVersion);
            }

            /// <inheritdoc/>
            public sealed override int GetHashCode()
            {
                return HashCode.Combine(this.Type, this.minVersion, this.maxVersion, this.minVersionExclusive, this.maxVersionExclusive);
            }

            /// <inheritdoc/>
            public sealed override string ToString()
            {
                Span<char> dest = stackalloc char[Range.BufferSize];
                bool success = this.TryFormat(dest, out int charsWritten);
                Debug.Assert(success);
                return dest.Slice(0, charsWritten).ToString();
            }

            /// <inheritdoc/>
            public bool TryFormat(Span<char> destination, out int charsWritten)
            {
                int totalCharsWritten = 0;
                
                if (!this.minVersion.TryFormat(destination, out int valueCharsWritten))
                {
                    totalCharsWritten += valueCharsWritten;
                    destination = destination.Slice(valueCharsWritten);
                }

                // this gets rid of IsEmpty checks.
                // worst case, both exclusive (3 chars)
                // A version cannot be only 2 chars, so this
                // works out fine.
                if (destination.Length <= 5)
                {
                    charsWritten = 0;
                    return false;
                }

                destination[0] = '<';
                destination = destination.Slice(1);

                if (!this.minVersionExclusive)
                {
                    destination[0] = '=';
                    destination = destination.Slice(1);
                    totalCharsWritten++;
                }

                destination[0] = 'n';
                destination[1] = '<';
                destination = destination.Slice(2);

                if (!this.maxVersionExclusive)
                {
                    destination[0] = '=';
                    destination = destination.Slice(1);
                    totalCharsWritten++;
                }

                totalCharsWritten += 3;

                if (this.maxVersion.TryFormat(destination, out valueCharsWritten))
                {
                    charsWritten = valueCharsWritten + totalCharsWritten;
                    return true;
                }
                else
                {
                    charsWritten = 0;
                    return false;
                }
            }

            // Version
            // <= - 2 characters
            // n - 1 character
            // <= - 2 characters
            // Version
            private const int BufferSize = (ModReferenceVersion.BufferSize * 2) + 5;
        }
    }
}
