using System;

namespace MipInEx;

/// <summary>
/// The information about a root plugin in a mod assembly.
/// </summary>
public sealed class ModAssemblyRootPluginInfo : IModAssemblyPluginInfo
{
    private ModAssemblyInfo assembly;
    private readonly ModRootPluginMetadata metadata;
    private bool isLoaded;

    internal ModAssemblyRootPluginInfo(ModRootPluginMetadata metadata)
    {
        this.assembly = null!;
        this.metadata = metadata;
    }

    /// <inheritdoc cref="ModAssemblyRootPlugin.Name"/>
    public string Name => this.metadata.Name;

    /// <inheritdoc cref="ModAssemblyRootPlugin.Guid"/>
    public string Guid => this.metadata.Guid;

    /// <inheritdoc cref="ModAssemblyRootPlugin.Version"/>
    public Version Version => this.metadata.Version;

    /// <inheritdoc cref="ModAssemblyRootPlugin.FullGuid"/>
    public string FullGuid => this.metadata.FullGuid;

    /// <inheritdoc cref="ModAssemblyRootPlugin.IsInternalPlugin"/>
    public bool IsInternalPlugin => false;

    /// <inheritdoc cref="ModAssemblyRootPlugin.Mod"/>
    public ModInfo Mod => this.assembly.Mod;

    /// <inheritdoc cref="ModAssemblyRootPlugin.Assembly"/>
    public ModAssemblyInfo Assembly => this.assembly;

    /// <inheritdoc cref="ModAssemblyRootPlugin.Metadata"/>
    public ModRootPluginMetadata Metadata => this.metadata;
    ModPluginMetadata IModAssemblyPluginInfo.Metadata => this.metadata;

    /// <inheritdoc cref="ModAssemblyRootPlugin.IsLoaded"/>
    public bool IsLoaded => this.isLoaded;

    /// <inheritdoc cref="ModAssemblyRootPlugin.GetDescriptorString()"/>
    public string GetDescriptorString()
    {
        return $"Plugin '{this.Name}'";
    }

    /// <inheritdoc cref="ModAssemblyRootPlugin.ToString()"/>
    public sealed override string ToString()
    {
        return this.GetDescriptorString();
    }

    internal void SetLoaded(bool loaded)
    {
        this.isLoaded = loaded;
    }

    internal void Initialize(ModAssemblyInfo assembly)
    {
        this.assembly = assembly;
    }
}
