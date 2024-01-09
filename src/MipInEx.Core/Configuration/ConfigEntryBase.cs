using System;
using System.IO;
using MipInEx.Logging;

namespace MipInEx.Configuration;

/// <summary>
/// Provides access to a single setting inside of a
/// <see cref="Configuration.ConfigFile"/>.
/// </summary>
/// <typeparam name="T">Type of the setting.</typeparam>
public sealed class ConfigEntry<T> : ConfigEntryBase
{
    private T typedValue = default!;

    internal ConfigEntry(ConfigFile configFile, ConfigDefinition definition, T defaultValue, ConfigDescription? configDescription)
        : base(configFile, definition, typeof(T), defaultValue, configDescription)
    {
        this.ConfigFile.SettingChanged += this.OnConfigSettingChanged;
    }

    /// <summary>
    /// Value of this setting.
    /// </summary>
    public T Value
    {
        get => this.typedValue;
        set
        {
            value = this.ClampValue(value);

            if (value is null)
            {
                if (this.typedValue is null)
                {
                    return;
                }
            }
            else if (value.Equals(this.typedValue))
            {
                return;
            }

            this.typedValue = value;
            this.OnSettingChanged(this);
        }
    }

    /// <inheritdoc/>
    public override object? BoxedValue
    {
        get => this.Value;
        set => this.Value = (T)value!;
    }

    /// <summary>
    /// Fired when the setting is changed. Does not detect
    /// changes made outside from this object.
    /// </summary>
    public event EventHandler SettingChanged = null!;

    private void OnConfigSettingChanged(object sender, SettingChangedEventArgs args)
    {
        if (args.ChangedSetting == this)
        {
            this.SettingChanged?.Invoke(sender, args);
        }
    }
}

/// <summary>
/// Container for a single setting of a
/// <see cref="Configuration.ConfigFile"/>. Each config entry
/// is linked to one config file.
/// </summary>
public abstract class ConfigEntryBase
{
    private readonly ConfigFile configFile;
    private readonly ConfigDefinition definition;
    private readonly ConfigDescription description;
    private readonly Type settingType;
    private readonly object? defaultValue;

    /// <summary>
    /// Types of defaultValue and definition.AcceptableValues
    /// have to be the same as settingType.
    /// </summary>
    /// <param name="configFile"></param>
    /// <param name="definition"></param>
    /// <param name="settingType"></param>
    /// <param name="defaultValue"></param>
    /// <param name="configDescription"></param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configFile"/>,
    /// <paramref name="definition"/>, or
    /// <paramref name="settingType"/> are
    /// <see langword="null"/>.
    /// </exception>
    protected internal ConfigEntryBase(ConfigFile configFile, ConfigDefinition definition, Type settingType, object? defaultValue, ConfigDescription? configDescription)
    {
        this.configFile = configFile ?? throw new ArgumentNullException(nameof(configFile));
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.settingType = settingType ?? throw new ArgumentNullException(nameof(settingType));
        this.description = configDescription ?? ConfigDescription.Empty;
        if (this.description.AcceptableValues is not null &&
            !this.settingType.IsAssignableFrom(this.description.AcceptableValues.ValueType))
        {
            throw new ArgumentException("configDescription.AcceptableValues is for a different type than the type of this setting");
        }

        this.defaultValue = defaultValue;

        // Free type check and automatically calls ClampValue in case AcceptableValues were provided
        this.BoxedValue = defaultValue;
    }

    /// <summary>
    /// Config file this entry is a part of.
    /// </summary>
    public ConfigFile ConfigFile => this.configFile;

    /// <summary>
    /// Category and name of this setting. Used as a unique key
    /// for identification within a
    /// <see cref="Configuration.ConfigFile"/>.
    /// </summary>
    public ConfigDefinition Definition => this.definition;

    /// <summary>
    /// Description / metadata of this setting.
    /// </summary>
    public ConfigDescription Description => this.description;

    /// <summary>
    /// Type of the <see cref="BoxedValue"/> that this setting
    /// holds.
    /// </summary>
    public Type SettingType => this.settingType;

    /// <summary>
    /// Default value of this setting (set only if the setting
    /// was not changed before).
    /// </summary>
    public object? DefaultValue { get; }

    /// <summary>
    /// Get or set the value of the setting.
    /// </summary>
    public abstract object? BoxedValue { get; set; }

    /// <summary>
    /// Get the serialized representation of the value.
    /// </summary>
    /// <returns>
    /// The serialized string representation of the value.
    /// </returns>
    public string GetSerializedValue()
    {
        return TomlSerializer.Serialize(this.BoxedValue, this.settingType);
    }

    /// <summary>
    /// Set the value by using its serialized form.
    /// </summary>
    /// <param name="value">
    /// The serialized value.
    /// </param>
    public void SetSerializedValue(string value)
    {
        try
        {
            this.BoxedValue = TomlSerializer.Deserialize(value, this.settingType);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, $"Config value of setting \"{this.definition}\" could not be parsed and will be ignored. Reason: {ex.Message}; Value: {value}");
        }
    }

    /// <summary>
    /// If necessary, clamp the value to acceptable value
    /// range. T has to be equal to settingType.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value to clamp.
    /// </typeparam>
    /// <param name="value">
    /// The value to clamp.
    /// </param>
    /// <returns>
    /// The clamped value.
    /// </returns>
    protected T ClampValue<T>(T value)
    {
        if (this.description.AcceptableValues is not null)
        {
            return (T)this.description.AcceptableValues.Clamp(value)!;
        }
        return value;
    }

    /// <summary>
    /// Trigger setting changed event.
    /// </summary>
    /// <param name="sender">The sender of the evemt.</param>
    protected void OnSettingChanged(object sender)
    {
        this.configFile.OnSettingChanged(sender, this);
    }


    /// <summary>
    /// Write a description of this setting using all available
    /// metadata.
    /// </summary>
    /// <param name="writer">
    /// The writer to write to.
    /// </param>
    public void WriteDescription(StreamWriter writer)
    {
        if (!string.IsNullOrEmpty(this.description.Description))
            writer.WriteLine($"## {this.description.Description.Replace("\n", "\n## ")}");

        writer.WriteLine($"# Setting type: {this.settingType.Name}");
        writer.WriteLine($"# Default value: {TomlSerializer.Serialize(this.defaultValue, this.settingType)}");

        if (this.description.AcceptableValues is not null)
        {
            writer.WriteLine(this.description.AcceptableValues.ToDescriptionString());
        }
        else if (this.settingType.IsEnum)
        {
            string[] names = Enum.GetNames(this.settingType);

            writer.WriteLine($"# Acceptable values: {string.Join(", ", names)}");
            if (this.settingType.GetCustomAttributes(typeof(FlagsAttribute), true).Length > 0)
            {
                string exampleString = names.Length < 2 ?
                    "Debug, Warning" :
                    names[0] + ", " + names[1];

                writer.WriteLine($"# Multiple values can be set at the same time by separating them with , (e.g. {exampleString})");
            }
        }
    }
}
