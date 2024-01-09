using System;

namespace MipInEx;

/// <summary>
/// The representation of a plugin in a mod assembly.
/// </summary>
public interface IModAssemblyPlugin
{
    /// <summary>
    /// The name of this plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The guid of this plugin.
    /// </summary>
    string Guid { get; }

    /// <summary>
    /// The version of this plugin.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// The full guid of this plugin.
    /// </summary>
    string FullGuid { get; }

    /// <summary>
    /// Whether or not this is an internal plugin.
    /// </summary>
    /// <remarks>
    /// If <see langword="true"/>, then this represents an
    /// internal plugin. If <see langword="false"/>, then this
    /// represents a root plugin.
    /// </remarks>
    bool IsInternalPlugin { get; }

    /// <summary>
    /// The mod this plugin belongs to.
    /// </summary>
    Mod Mod { get; }

    /// <summary>
    /// The mod manager that manages the mod this plugin
    /// belongs to.
    /// </summary>
    ModManagerBase ModManager { get; }

    /// <summary>
    /// The assembly this plugin belongs to.
    /// </summary>
    ModAssembly Assembly { get; }

    /// <summary>
    /// The metadata of this plugin.
    /// </summary>
    ModPluginMetadata Metadata { get; }

    /// <summary>
    /// The info about this plugin.
    /// </summary>
    IModAssemblyPluginInfo Info { get; }

    /// <summary>
    /// Whether or not this plugin is loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the plugin instance.
    /// </summary>
    /// <returns>
    /// The plugin instance of this plugin.
    /// </returns>
    IModPluginInstance GetInstance();

    /// <summary>
    /// Gets the descriptor string for this plugin.
    /// </summary>
    /// <returns>
    /// The descriptor string of this plugin.
    /// </returns>
    string GetDescriptorString();

    /// <summary>
    /// Loads this plugin.
    /// </summary>
    void Load();

    /// <summary>
    /// Unloads this plugin.
    /// </summary>
    void Unload();
}
