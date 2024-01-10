namespace MipInEx;

/// <summary>
/// A mod root plugin instance.
/// </summary>
public interface IModRootPluginInstance : IModPluginInstance
{
    /// <inheritdoc cref="ModRootPluginInfo.Metadata"/>
    new ModRootPluginMetadata Metadata { get; }

    /// <summary>
    /// The root assembly plugin defining this root plugin.
    /// </summary>
    new ModAssemblyRootPlugin AssemblyPlugin { get; }
}

internal interface IModRootPluginInstanceImpl : IModRootPluginInstance, IModPluginInstanceImpl
{
    void Initialize(ModAssemblyRootPlugin assemblyPlugin);
}
