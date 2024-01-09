using System;

namespace MipInEx;

/// <summary>
/// A mod plugin instance.
/// </summary>
public interface IModPluginInstance
{
    /// <summary>
    /// The name of this plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The GUID of this plugin.
    /// </summary>
    string Guid { get; }

    /// <summary>
    /// The version of this plugin.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// The metadata of this plugin.
    /// </summary>
    ModPluginMetadata Metadata { get; }

    /// <summary>
    /// The assembly plugin defining this plugin.
    /// </summary>
    IModAssemblyPlugin AssemblyPlugin { get; }
}

internal interface IModPluginInstanceImpl : IModPluginInstance
{
    void Load();
    void Unload();
}
