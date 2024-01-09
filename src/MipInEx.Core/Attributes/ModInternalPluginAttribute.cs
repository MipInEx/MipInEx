using System;

namespace MipInEx;

/// <summary>
/// Attach to a class to specify as the main plugin for the
/// mod.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ModPluginInternalAttribute : Attribute
{
    private readonly string? guid;
    private readonly string? name;
    private readonly Version? version;

    /// <summary>
    /// Initializes this attribute with the specified guid,
    /// name, and version.
    /// </summary>
    /// <param name="guid">
    /// The unique global identifier of the plugin.
    /// </param>
    /// <param name="name">
    /// The name of the plugin.
    /// </param>
    /// <param name="version">
    /// The version of the plugin.
    /// </param>
    public ModPluginInternalAttribute(string guid, string name, string version)
    {
        this.name = name;
        this.guid = guid;
        Version.TryParse(version, out this.version);
    }

    /// <summary>
    /// The guid of the plugin.
    /// </summary>
    public string? Guid => this.guid;

    /// <summary>
    /// The name of the plugin.
    /// </summary>
    public string? Name => this.name;

    /// <summary>
    /// The version of the plugin.
    /// </summary>
    public Version? Version => this.version;
}
