using HarmonyLib;
using MipInEx.Logging;
using MipInEx.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// The base class for a mod's plugin.
/// </summary>
public abstract class ModRootPluginBase : IModRootPluginInstanceImpl
{
    private Harmony? patcher;
    private ModAssemblyRootPlugin? assemblyPlugin;
    private ConfigFile? config;
    private ManualLogSource? logger;

    /// <inheritdoc/>
    protected ModRootPluginBase()
    {
        this.patcher = null;
        this.assemblyPlugin = null;
        this.config = null;
        this.logger = null;
    }

    /// <inheritdoc cref="IModRootPluginInstance.Metadata"/>
    public ModRootPluginMetadata Metadata => this.assemblyPlugin!.Metadata;
    ModPluginMetadata IModPluginInstance.Metadata => this.assemblyPlugin!.Metadata;

    /// <inheritdoc cref="IModRootPluginInstance.AssemblyPlugin"/>
    public ModAssemblyRootPlugin AssemblyPlugin => this.assemblyPlugin!;
    IModAssemblyPlugin IModPluginInstance.AssemblyPlugin => this.assemblyPlugin!;

    /// <inheritdoc cref="ModAssemblyRootPlugin.Assembly"/>
    public ModAssembly Assembly => this.assemblyPlugin!.Assembly;
    /// <inheritdoc cref="ModAssemblyRootPlugin.ModManager"/>
    public ModManagerBase ModManager => this.assemblyPlugin!.ModManager;

    /// <inheritdoc cref="IModPluginInstance.Name"/>
    public string Name => this.assemblyPlugin!.Metadata.Name;
    /// <inheritdoc cref="IModPluginInstance.Guid"/>
    public string Guid => this.assemblyPlugin!.Metadata.Guid;
    /// <inheritdoc cref="IModPluginInstance.Version"/>
    public Version Version => this.assemblyPlugin!.Metadata.Version;

    /// <summary>
    /// The patcher for this root plugin.
    /// </summary>
    public Harmony Patcher => this.patcher!;

    /// <summary>
    /// The config file.
    /// </summary>
    public ConfigFile Config => this.config!;

    /// <summary>
    /// The logger for this root plugin.
    /// </summary>
    public ManualLogSource Logger => this.logger!;

    /// <summary>
    /// Loads this plugin.
    /// </summary>
    protected abstract void Load();
    void IModPluginInstanceImpl.Load()
        => this.Load();

    /// <summary>
    /// Unloads this plugin.
    /// </summary>
    protected virtual void Unload()
    { }
    void IModPluginInstanceImpl.Unload()
        => this.Unload();

    /// <summary>
    /// Loads the internal plugin with the specified GUID.
    /// </summary>
    /// <param name="guid">
    /// The GUID of the internal plugin to load.
    /// </param>
    protected void LoadInternalPlugin(string guid)
    {
        this.Assembly.InternalPlugins.Load(guid);
    }

    /// <summary>
    /// Unloads the internal plugin with the specified GUID.
    /// </summary>
    /// <param name="guid">
    /// The GUID of the internal plugin to unload.
    /// </param>
    protected void UnloadInternalPlugin(string guid)
    {
        this.Assembly.InternalPlugins.Unload(guid);
    }

    void IModRootPluginInstanceImpl.Initialize(ModAssemblyRootPlugin assemblyPlugin)
    {
        if (this.assemblyPlugin is not null) return;
        this.assemblyPlugin = assemblyPlugin;
        this.patcher = new Harmony(this.Guid);
        this.config = new ConfigFile(System.IO.Path.Combine(assemblyPlugin.Mod.ModManager.Paths.ConfigDirectory, this.Guid + ".cfg"), false, assemblyPlugin.Metadata);
        this.logger = MipInEx.Logging.Logger.CreateLogSource(this.Name);
    }
}
