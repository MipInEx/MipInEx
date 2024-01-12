namespace MipInEx;

/// <summary>
/// A mod internal plugin instance.
/// </summary>
public interface IModInternalPluginInstance : IModPluginInstance
{
    /// <summary>
    /// The root plugin the internal plugin is for.
    /// </summary>
    IModRootPluginInstance RootPlugin { get; }

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Metadata"/>
    new ModInternalPluginMetadata Metadata { get; }

    /// <summary>
    /// The internal assembly plugin defining this internal
    /// plugin.
    /// </summary>
    new ModAssemblyInternalPlugin AssemblyPlugin { get; }
}

internal interface IModInternalPluginInstanceImpl : IModInternalPluginInstance, IModPluginInstanceImpl
{
    void Initialize(ModAssemblyInternalPlugin assemblyPlugin);
}
