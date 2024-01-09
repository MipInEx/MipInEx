using MipInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace MipInEx.Configuration;

/// <summary>
/// A helper class to handle persistent data. All public
/// methods are thread-safe.
/// </summary>
public class ConfigFile : 
    IDictionary<ConfigDefinition, ConfigEntryBase>,
    IReadOnlyDictionary<ConfigDefinition, ConfigEntryBase>
{
    private readonly ModPluginMetadata? ownerMetadata;

    /// <summary>
    /// All config entries inside
    /// </summary>
    protected readonly Dictionary<ConfigDefinition, ConfigEntryBase> entries = new();

    private readonly Dictionary<ConfigDefinition, string> orphanedEntries = new();

    /// <inheritdoc cref="ConfigFile(string, bool, ModPluginMetadata?)"/>
    public ConfigFile(string configPath, bool saveOnInit)
        : this(configPath, saveOnInit, null)
    { }

    /// <summary>
    /// Create a new config file at the specified config path.
    /// </summary>
    /// <param name="configPath">
    /// Full path to a file that contains settings. The file
    /// will be created as needed.
    /// </param>
    /// <param name="saveOnInit">
    /// If the config file/directory doesn't exist, create it
    /// immediately.
    /// </param>
    /// <param name="ownerMetadata">
    /// Information about the plugin that owns this setting file.
    /// </param>
    public ConfigFile(string configPath, bool saveOnInit, ModPluginMetadata? ownerMetadata)
    {
        this.ownerMetadata = ownerMetadata;

        if (configPath is null) throw new ArgumentNullException(nameof(configPath));

        this.ConfigFilePath = Path.GetFullPath(configPath);

        if (File.Exists(this.ConfigFilePath))
        {
            this.Reload();
        }
        else if (saveOnInit)
        {
            this.Save();
        }
    }

    /// <summary>
    /// Full path to the config file. The file might not exist
    /// until a setting is added and changed, or
    /// <see cref="Save"/> is called.
    /// </summary>
    public string ConfigFilePath { get; }

    /// <summary>
    /// If enabled, writes the config to disk every time a
    /// value is set. If disabled, you have to manually use
    /// <see cref="Save"/> or the changes will be lost!
    /// </summary>
    public bool SaveOnConfigSet { get; set; } = true;

    /// <inheritdoc/>
    public ConfigEntryBase this[ConfigDefinition key]
    {
        get
        {
            lock (this.ioLock)
            {
                return this.entries[key];
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="section"></param>
    /// <param name="key"></param>
    public ConfigEntryBase this[string section, string key]
        => this[new ConfigDefinition(section, key)];

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<ConfigDefinition, ConfigEntryBase>> GetEnumerator()
        // We can't really do a read lock for this
        => this.entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    void ICollection<KeyValuePair<ConfigDefinition, ConfigEntryBase>>.Add(KeyValuePair<ConfigDefinition, ConfigEntryBase> item)
    {
        lock (this.ioLock)
        {
            this.entries.Add(item.Key, item.Value);
        }
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<ConfigDefinition, ConfigEntryBase> item)
    {
        lock (this.ioLock)
        {
            return ((ICollection<KeyValuePair<ConfigDefinition, ConfigEntryBase>>)this.entries).Contains(item);
        }
    }

    void ICollection<KeyValuePair<ConfigDefinition, ConfigEntryBase>>.CopyTo(KeyValuePair<ConfigDefinition, ConfigEntryBase>[] array, int arrayIndex)
    {
        lock (this.ioLock)
        {
            ((ICollection<KeyValuePair<ConfigDefinition, ConfigEntryBase>>)this.entries).CopyTo(array, arrayIndex);
        }
    }

    bool ICollection<KeyValuePair<ConfigDefinition, ConfigEntryBase>>.Remove(KeyValuePair<ConfigDefinition, ConfigEntryBase> item)
    {
        lock (this.ioLock)
        {
            return this.entries.Remove(item.Key);
        }
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            lock (this.ioLock)
            {
                return this.entries.Count;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public bool ContainsKey(ConfigDefinition key)
    {
        lock (this.ioLock)
        {
            return this.entries.ContainsKey(key);
        }
    }

    /// <inheritdoc/>
    public void Add(ConfigDefinition key, ConfigEntryBase value)
        => throw new NotSupportedException("Directly adding a config entry is not supported");

    /// <inheritdoc/>
    public bool Remove(ConfigDefinition key)
    {
        lock (this.ioLock)
        {
            return this.entries.Remove(key);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (this.ioLock)
        {
            this.entries.Clear();
        }
    }

    bool IDictionary<ConfigDefinition, ConfigEntryBase>.TryGetValue(ConfigDefinition key, out ConfigEntryBase value)
    {
        lock (this.ioLock)
        {
            return this.entries.TryGetValue(key, out value);
        }
    }

    bool IReadOnlyDictionary<ConfigDefinition, ConfigEntryBase>.TryGetValue(ConfigDefinition key, out ConfigEntryBase value)
    {
        lock (this.ioLock)
        {
            return this.entries.TryGetValue(key, out value);
        }
    }

    ConfigEntryBase IDictionary<ConfigDefinition, ConfigEntryBase>.this[ConfigDefinition key]
    {
        get
        {
            lock (this.ioLock)
            {
                return this.entries[key];
            }
        }
        set => throw new NotSupportedException("Directly setting a config entry is not supported");
    }

    /// <summary>
    /// Returns the ConfigDefinitions that the ConfigFile
    /// contains.
    /// <para>
    /// Creates a new array when the property is accessed.
    /// Thread-safe.
    /// </para>
    /// </summary>
    public ICollection<ConfigDefinition> Keys
    {
        get
        {
            lock (this.ioLock)
            {
                return this.entries.Keys.ToArray();
            }
        }
    }

    IEnumerable<ConfigDefinition> IReadOnlyDictionary<ConfigDefinition, ConfigEntryBase>.Keys
    {
        get => this.Keys;
    }

    /// <summary>
    /// Returns the ConfigEntryBase values that the ConfigFile
    /// contains.
    /// <para>
    /// Creates a new array when the property is accessed.
    /// Thread-safe.
    /// </para>
    /// </summary>
    public ICollection<ConfigEntryBase> Values
    {
        get
        {
            lock (this.ioLock)
            {
                return this.entries.Values.ToArray();
            }
        }
    }

    IEnumerable<ConfigEntryBase> IReadOnlyDictionary<ConfigDefinition, ConfigEntryBase>.Values
    {
        get => this.Values;
    }

    #region Save/Load
    private readonly object ioLock = new();

    /// <summary>
    /// Generate user-readable comments for each of the
    /// settings in the saved .cfg file.
    /// </summary>
    public bool GenerateSettingDescriptions { get; set; } = true;

    /// <summary>
    /// Reloads the config from disk. Unsaved changes are lost.
    /// </summary>
    public void Reload()
    {
        lock (this.ioLock)
        {
            this.orphanedEntries.Clear();
            string currentSection = string.Empty;

            // we use spans here to avoid unneeded allocations.
            // todo: optimize into ReadAllText and put line string into readonly span.
            foreach (string rawLine in File.ReadAllLines(this.ConfigFilePath))
            {
                ReadOnlySpan<char> line = rawLine.AsSpan().Trim();

                if (line.Length == 0)
                    continue;

                if (line[0] == '#') // comment
                    continue;

                if (line[0] == '[' && line[line.Length - 1] == ']') // section
                {
                    currentSection = line.Slice(1, line.Length - 2).ToString();
                    continue;
                }

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex < 0)
                    continue; // invalid line

                string currentKey = line.Slice(0, equalsIndex).Trim().ToString();
                string currentValue = line.Slice(equalsIndex + 1).Trim().ToString();

                ConfigDefinition definition = new(currentSection, currentKey);

                if (this.entries.TryGetValue(definition, out ConfigEntryBase? entry))
                {
                    entry.SetSerializedValue(currentValue);
                }
                else
                {
                    this.orphanedEntries[definition] = currentValue;
                }
            }
        }

        this.OnConfigReloaded();
    }

    /// <summary>
    /// Writes the config to disk.
    /// </summary>
    public void Save()
    {
        lock (this.ioLock)
        {
            string? directoryName = Path.GetDirectoryName(this.ConfigFilePath);
            if (directoryName is not null)
            {
                Directory.CreateDirectory(directoryName);
            }

            using StreamWriter writer = new StreamWriter(this.ConfigFilePath, false, Utility.UTF8NoBom);

            if (this.ownerMetadata != null)
            {
                writer.WriteLine($"## Settings file was created by {this.ownerMetadata.ToInfoString()}");
                writer.WriteLine($"## Plugin GUID: {this.ownerMetadata.FullGuid}");
                writer.WriteLine();
            }

            IEnumerable<SerializedConfigEntry> allConfigEntries = this.entries
                .Select(x => new SerializedConfigEntry(x))
                .Concat(this.orphanedEntries.Select(x => new SerializedConfigEntry(x)));

            foreach (IGrouping<string, SerializedConfigEntry> section in allConfigEntries.GroupBy(x => x.Key.Section).OrderBy(x => x.Key))
            {
                // Section heading
                writer.WriteLine($"[{section.Key}]");

                foreach (SerializedConfigEntry serializedEntry in section)
                {
                    if (this.GenerateSettingDescriptions)
                    {
                        writer.WriteLine();
                        serializedEntry.Entry?.WriteDescription(writer);
                    }

                    writer.WriteLine($"{serializedEntry.Key.Key} = {serializedEntry.Value}");
                }

                writer.WriteLine();
            }
        }
    }

    private sealed class SerializedConfigEntry
    {
        public ConfigDefinition Key { get; }
        public ConfigEntryBase? Entry { get; }
        public string Value { get; }

        public SerializedConfigEntry(KeyValuePair<ConfigDefinition, ConfigEntryBase> entry)
            : this(entry.Key, entry.Value, entry.Value.GetSerializedValue())
        { }

        public SerializedConfigEntry(KeyValuePair<ConfigDefinition, string> entry)
            : this(entry.Key, null, entry.Value)
        { }

        public SerializedConfigEntry(ConfigDefinition key, ConfigEntryBase? entry, string value)
        {
            this.Key = key;
            this.Entry = entry;
            this.Value = value;
        }
    }

    #endregion

    #region Wraps

    /// <summary>
    /// Access one of the existing settings. If the setting has
    /// not been added yet, false is returned. Otherwise, true.
    /// If the setting exists but has a different type than T,
    /// an exception is thrown. New settings should be added
    /// with
    /// <see cref="Bind{T}(ConfigDefinition, T, ConfigDescription)"/>
    /// </summary>
    /// <typeparam name="T">
    /// Type of the value contained in this setting.
    /// </typeparam>
    /// <param name="configDefinition">
    /// Section and Key of the setting.
    /// </param>
    /// <param name="entry">
    /// The ConfigEntry value to return.
    /// </param>
    /// <returns>
    /// Whether or not fetching the entry was successful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configDefinition"/> is
    /// <see langword="null"/>.
    /// </exception>
    public bool TryGetEntry<T>(ConfigDefinition configDefinition, [NotNullWhen(true)] out ConfigEntry<T>? entry)
    {
        lock (this.ioLock)
        {
            if (this.entries.TryGetValue(configDefinition, out ConfigEntryBase? rawEntry))
            {
                entry = (ConfigEntry<T>)rawEntry;
                return true;
            }

            entry = null;
            return false;
        }
    }

    /// <summary>
    /// Access one of the existing settings. If the setting has
    /// not been added yet, false is returned. Otherwise, true.
    /// If the setting exists but has a different type than T,
    /// an exception is thrown. New settings should be added
    /// with
    /// <see cref="Bind{T}(ConfigDefinition, T, ConfigDescription)"/>
    /// </summary>
    /// <typeparam name="T">
    /// Type of the value contained in this setting.
    /// </typeparam>
    /// <param name="section">
    /// Section/category/group of the setting. Settings are
    /// grouped by this.
    /// </param>
    /// <param name="key">
    /// Name of the setting.
    /// </param>
    /// <param name="entry">
    /// The ConfigEntry value to return.
    /// </param>
    /// <returns>
    /// Whether or not fetching the entry was successful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="section"/> or
    /// <paramref name="key"/> are
    /// <see langword="null"/>.
    /// </exception>
    public bool TryGetEntry<T>(string section, string key, [NotNullWhen(true)] out ConfigEntry<T>? entry)
        => this.TryGetEntry(new ConfigDefinition(section, key), out entry);

    /// <summary>
    /// Create a new setting. The setting is saved to drive and
    /// loaded automatically. Each definition can be used to
    /// add only one setting, trying to add a second setting
    /// will just return the existing setting casted to a
    /// <see cref="ConfigEntry{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the value contained in this setting.
    /// </typeparam>
    /// <param name="configDefinition">
    /// Section and Key of the setting.
    /// </param>
    /// <param name="defaultValue">
    /// Value of the setting if the setting was not created
    /// yet.
    /// </param>
    /// <param name="configDescription">
    /// Description of the setting shown to the user and other
    /// metadata.
    /// </param>
    /// <returns>
    /// The created config entry, or the existing config entry.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="T"/> cannot be serialized.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configDefinition"/> is
    /// <see langword="null"/>.
    /// </exception>
    public ConfigEntry<T> Bind<T>(ConfigDefinition configDefinition, T defaultValue, ConfigDescription? configDescription = null)
    {
        if (!TomlSerializer.CanConvert(typeof(T)))
            throw new ArgumentException($"Type {typeof(T)} is not supported by the config system.");

        lock (this.ioLock)
        {
            if (this.entries.TryGetValue(configDefinition, out ConfigEntryBase? rawEntry))
                return (ConfigEntry<T>)rawEntry;

            ConfigEntry<T> entry = new(this, configDefinition, defaultValue, configDescription);

            this.entries[configDefinition] = entry;

            if (this.orphanedEntries.TryGetValue(configDefinition, out string? homelessValue))
            {
                entry.SetSerializedValue(homelessValue);
                this.orphanedEntries.Remove(configDefinition);
            }

            if (this.SaveOnConfigSet)
                this.Save();

            return entry;
        }
    }

    /// <summary>
    /// Create a new setting. The setting is saved to drive and
    /// loaded automatically. Each section and key pair can be
    /// used to add only one setting, trying to add a second
    /// setting will just return the existing setting casted to
    /// a <see cref="ConfigEntry{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the value contained in this setting.
    /// </typeparam>
    /// <param name="section">
    /// Section/category/group of the setting. Settings are grouped by this.
    /// </param>
    /// <param name="key">
    /// Name of the setting.
    /// </param>
    /// <param name="defaultValue">
    /// Value of the setting if the setting was not created
    /// yet.
    /// </param>
    /// <param name="configDescription">
    /// Description of the setting shown to the user and other
    /// metadata.
    /// </param>
    /// <returns>
    /// The created config entry, or the existing config entry.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="T"/> cannot be serialized.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="section"/> or
    /// <paramref name="key"/> are
    /// <see langword="null"/>.
    /// </exception>
    public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription? configDescription = null)
    {
        return this.Bind(new ConfigDefinition(section, key), defaultValue, configDescription);
    }

    /// <summary>
    /// Create a new setting. The setting is saved to drive and
    /// loaded automatically. Each section and key pair can be
    /// used to add only one setting, trying to add a second
    /// setting will just return the existing setting casted to
    /// a <see cref="ConfigEntry{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the value contained in this setting.
    /// </typeparam>
    /// <param name="section">
    /// Section/category/group of the setting. Settings are grouped by this.
    /// </param>
    /// <param name="key">
    /// Name of the setting.
    /// </param>
    /// <param name="defaultValue">
    /// Value of the setting if the setting was not created
    /// yet.
    /// </param>
    /// <param name="description">
    /// Simple description of the setting shown to the user.
    /// </param>
    /// <returns>
    /// The created config entry, or the existing config entry.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="T"/> cannot be serialized.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="section"/>,
    /// <paramref name="key"/>, or
    /// <paramref name="description"/> are
    /// <see langword="null"/>.
    /// </exception>
    public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
    {
        return this.Bind(new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description));
    }

    #endregion

    #region Events
    /// <summary>
    /// An event that is fired every time the config is reloaded.
    /// </summary>
    public event EventHandler ConfigReloaded = null!;

    /// <summary>
    /// Fired when one of the settings is changed.
    /// </summary>
    public event EventHandler<SettingChangedEventArgs> SettingChanged = null!;

    internal void OnSettingChanged(object sender, ConfigEntryBase changedEntryBase)
    {
        if (changedEntryBase is null)
            throw new ArgumentNullException(nameof(changedEntryBase));

        if (this.SaveOnConfigSet)
            this.Save();

        EventHandler<SettingChangedEventArgs>? settingChanged = this.SettingChanged;
        if (settingChanged == null)
        {
            return;
        }

        SettingChangedEventArgs args = new(changedEntryBase);
        foreach (EventHandler<SettingChangedEventArgs> callback in settingChanged.GetInvocationList())
        {
            try
            {
                callback.Invoke(sender, args);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex);
            }
        }
    }

    private void OnConfigReloaded()
    {
        EventHandler? configReloaded = this.ConfigReloaded;
        if (configReloaded == null)
        {
            return;
        }

        foreach (EventHandler callback in configReloaded.GetInvocationList())
        {
            try
            {
                callback.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex);
            }
        }
    }

    #endregion
}
