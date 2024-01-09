using System;

namespace MipInEx;

/// <summary>
/// The information about an internal plugin in a mod assembly.
/// </summary>
public sealed class ModAssemblyInternalPluginInfo : IModAssemblyPluginInfo
{
    private ModAssemblyInfo assembly;
    private readonly ModInternalPluginMetadata metadata;
    private bool isLoaded;

    internal ModAssemblyInternalPluginInfo(ModInternalPluginMetadata metadata)
    {
        this.assembly = null!;
        this.metadata = metadata;
        this.isLoaded = false;
    }

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Name"/>
    public string Name => this.metadata.Name;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Guid"/>
    public string Guid => this.metadata.Guid;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Version"/>
    public Version Version => this.metadata.Version;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.RootName"/>
    public string RootName => this.metadata.RootMetadata.Name;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.RootGuid"/>
    public string RootGuid => this.metadata.RootMetadata.Guid;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.RootVersion"/>
    public Version RootVersion => this.metadata.RootMetadata.Version;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.FullGuid"/>
    public string FullGuid => this.metadata.FullGuid;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.IsInternalPlugin"/>
    public bool IsInternalPlugin => true;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Mod"/>
    public ModInfo Mod => this.assembly.Mod;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Assembly"/>
    public ModAssemblyInfo Assembly => this.assembly;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Metadata"/>
    public ModInternalPluginMetadata Metadata => this.metadata;
    ModPluginMetadata IModAssemblyPluginInfo.Metadata => this.metadata;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.IsLoaded"/>
    public bool IsLoaded => this.isLoaded;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.GetDescriptorString()"/>
    public string GetDescriptorString()
    {
        return $"Internal Plugin '{this.Name}'";
    }

    /// <inheritdoc cref="ModAssemblyInternalPlugin.ToString()"/>
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