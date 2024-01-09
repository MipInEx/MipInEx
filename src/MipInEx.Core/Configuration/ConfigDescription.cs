using System;

namespace MipInEx.Configuration;

/// <summary>
/// Metadata of a <see cref="ConfigEntryBase"/>.
/// </summary>
public sealed class ConfigDescription
{
    private readonly string description;
    private readonly IAcceptableValue? acceptableValues;
    private readonly object?[] tags;

    /// <summary>
    /// Create a new description.
    /// </summary>
    /// <param name="description">
    /// Text describing the function of the setting and any
    /// notes or warnings.
    /// </param>
    /// <param name="acceptableValues">
    /// Range of values that this setting can take. The
    /// setting's value will be automatically clamped.
    /// </param>
    /// <param name="tags">
    /// Objects that can be used by user-made classes to add
    /// functionality.
    /// </param>
    public ConfigDescription(string description, IAcceptableValue? acceptableValues = null, params object?[] tags)
    {
        this.description = description ?? throw new ArgumentNullException(nameof(description));
        this.acceptableValues = acceptableValues;
        this.tags = tags;
    }

    /// <summary>
    /// Text describing the function of the setting and any
    /// notes or warnings.
    /// </summary>
    public string Description => this.description;

    /// <summary>
    /// Range of acceptable values for a setting.
    /// </summary>
    public IAcceptableValue? AcceptableValues => this.acceptableValues;

    /// <summary>
    /// Objects that can be used by user-made classes to add
    /// functionality.
    /// </summary>
    public object?[] Tags => this.tags;

    /// <summary>
    /// An empty description.
    /// </summary>
    public static ConfigDescription Empty { get; } = new("");
}
