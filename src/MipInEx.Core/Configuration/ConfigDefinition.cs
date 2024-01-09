using System;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx.Configuration;

/// <summary>
/// Section and key of a setting. Used as a unique key for
/// identification within a <see cref="ConfigFile"/>. The same
/// definition can be used in multiple config files, it will
/// point to different settings then.
/// </summary>
public sealed class ConfigDefinition : IEquatable<ConfigDefinition>
{
    private readonly string section;
    private readonly string key;

    /// <summary>
    /// Create a new definition. Definitions with same section
    /// and key are equal.
    /// </summary>
    /// <param name="section">
    /// Group of the setting, case sensitive.
    /// </param>
    /// <param name="key">
    /// Name of the setting, case sensitive.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="section"/> or
    /// <paramref name="key"/> are
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="section"/> or
    /// <paramref name="key"/> contain invalid characters.
    /// </exception>
    public ConfigDefinition(string section, string key)
    {
        ConfigDefinition.CheckInvalidConfigChars(section, nameof(section));
        ConfigDefinition.CheckInvalidConfigChars(key, nameof(key));

        this.section = section;
        this.key = key;
    }

    /// <summary>
    /// Group of the setting. All settings within a config file
    /// are grouped by this.
    /// </summary>
    public string Section => this.section;

    /// <summary>
    /// Name of the setting.
    /// </summary>
    public string Key => this.key;

    /// <summary>
    /// Checks if the definitions are the same.
    /// </summary>
    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (object.ReferenceEquals(obj, this)) return true;
        else return this.Equals(obj as ConfigDefinition);
    }

    /// <summary>
    /// Checks if the definitions are the same.
    /// </summary>
    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals([NotNullWhen(true)] ConfigDefinition? other)
    {
        return other is not null &&
            this.key == other.key &&
            this.section == other.section;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.section, this.key);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return this.section + "." + this.key;
    }

    private static void CheckInvalidConfigChars([NotNull] string value, string paramName)
    {
        if (value is null) throw new ArgumentNullException(paramName);
        else if (value.Length == 0) throw new ArgumentException("Section and key names cannot be an empty string", paramName);
        else if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])) throw new ArgumentException("Cannot use whitespace characters at start or end of section and key names", paramName);

        foreach (char c in value)
        {
            if (c == '=' || c == '\n' || c == '\t' || c == '\\' || c == '"' || c == '\'' || c == '[' || c == ']')
            {
                throw new ArgumentException("Cannot use any of the following characters in section and key names: = \\n \\t \\ \"\" ' [ ]", paramName);
            }
        }
    }

    /// <summary>
    /// Checks if the definitions are the same.
    /// </summary>
    /// <param name="left">
    /// The left config definition.
    /// </param>
    /// <param name="right">
    /// The right config definition.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the values are equal;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool operator ==(ConfigDefinition? left, ConfigDefinition? right)
    {
        if (right is null) return left is null;
        else return right.Equals(left);
    }

    /// <summary>
    /// Checks if the definitions aren't the same.
    /// </summary>
    /// <param name="left">
    /// The left config definition.
    /// </param>
    /// <param name="right">
    /// The right config definition.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the values aren't equal;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool operator !=(ConfigDefinition? left, ConfigDefinition? right)
    {
        if (right is null) return left is not null;
        else return !right.Equals(left);
    }
}
