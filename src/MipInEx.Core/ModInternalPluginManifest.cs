using System;

namespace MipInEx;

/// <summary>
/// The manifest details for an internal plugin.
/// </summary>
public sealed class ModInternalPluginManifest
{
    private readonly string guid;
    private readonly long loadPriority;
    private readonly bool loadManually;

    /// <summary>
    /// Initializes this internal plugin settings with the
    /// internal plugin guid, priority and whether or not to
    /// load the internal plugin manually.
    /// </summary>
    /// <param name="guid">
    /// The guid of the internal plugin.
    /// </param>
    /// <param name="loadPriority">
    /// The load priority of this internal plugin. The higher
    /// the value, the higher the priority.
    /// </param>
    /// <param name="loadManually">
    /// Whether or not this internal plugin needs to be
    /// manually loaded.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    public ModInternalPluginManifest(string guid, long loadPriority, bool loadManually)
    {
        if (guid is null)
            throw new ArgumentNullException(nameof(guid));

        this.guid = guid;
        this.loadPriority = loadPriority;
        this.loadManually = loadManually;
    }

    /// <summary>
    /// The GUID of the internal plugin.
    /// </summary>
    public string Guid => this.guid;

    /// <summary>
    /// The load priority of this internal plugin. The higher
    /// the value, the higher the priority.
    /// </summary>
    /// <remarks>
    /// Internal plugins not specified will have a load
    /// priority of <c>0</c>.
    /// </remarks>
    public long LoadPriority => this.loadPriority;

    /// <summary>
    /// Whether or not this internal plugin needs to be
    /// explicitly loaded (aka manually loaded)
    /// </summary>
    /// <remarks>
    /// If set to load automatically, then this internal plugin
    /// will load <b>AFTER</b> the root plugin loads.
    /// </remarks>
    public bool LoadManually => this.loadManually;
}
