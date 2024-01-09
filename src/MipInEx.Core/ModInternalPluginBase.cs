using HarmonyLib;
using MipInEx.Logging;
using MipInEx.Configuration;
using System;

namespace MipInEx;

/// <summary>
/// The base class for a mod's internal plugin.
/// </summary>
public abstract class ModInternalPluginBase : IModInternalPluginInstanceImpl
{
    private ModRootPluginBase? root;
    private Harmony? patcher;
    private ModAssemblyInternalPlugin? assemblyPlugin;
    private ConfigFile? config;
    private ManualLogSource? logger;

    /// <inheritdoc/>
    protected ModInternalPluginBase()
    {
        this.root = null;
        this.patcher = null;
        this.assemblyPlugin = null;
        this.config = null;
        this.logger = null;
    }

    /// <inheritdoc cref="IModInternalPluginInstance.Metadata"/>
    public ModInternalPluginMetadata Metadata => this.assemblyPlugin!.Metadata;
    ModPluginMetadata IModPluginInstance.Metadata => this.assemblyPlugin!.Metadata;

    /// <inheritdoc cref="IModInternalPluginInstance.AssemblyPlugin"/>
    public ModAssemblyInternalPlugin AssemblyPlugin => this.assemblyPlugin!;
    IModAssemblyPlugin IModPluginInstance.AssemblyPlugin => this.assemblyPlugin!;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Assembly"/>
    public ModAssembly Assembly => this.assemblyPlugin!.Assembly;
    /// <inheritdoc cref="ModAssemblyInternalPlugin.ModManager"/>
    public ModManagerBase ModManager => this.assemblyPlugin!.ModManager;

    /// <inheritdoc cref="IModPluginInstance.Name"/>
    public string Name => this.assemblyPlugin!.Metadata.Name;
    /// <inheritdoc cref="IModPluginInstance.Guid"/>
    public string Guid => this.assemblyPlugin!.Metadata.Guid;
    /// <inheritdoc cref="IModPluginInstance.Version"/>
    public Version Version => this.assemblyPlugin!.Metadata.Version;

    /// <summary>
    /// The patcher for this internal plugin.
    /// </summary>
    public Harmony Patcher => this.patcher!;

    /// <summary>
    /// The config file.
    /// </summary>
    public ConfigFile Config => this.config!;

    /// <summary>
    /// The logger for this internal plugin.
    /// </summary>
    public ManualLogSource Logger => this.logger!;

    /// <inheritdoc cref="IModInternalPluginInstance.RootPlugin"/>
    public ModRootPluginBase Root => this.root!;
    IModRootPluginInstance IModInternalPluginInstance.RootPlugin => this.root!;

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

    /// <inheritdoc cref="ModRootPluginBase.LoadInternalPlugin(string)"/>
    protected void LoadInternalPlugin(string guid)
    {
        this.Assembly.InternalPlugins.Load(guid);
    }

    /// <inheritdoc cref="ModRootPluginBase.UnloadInternalPlugin(string)"/>
    protected void UnloadInternalPlugin(string guid)
    {
        this.Assembly.InternalPlugins.Unload(guid);
    }

    void IModInternalPluginInstanceImpl.Initialize(ModAssemblyInternalPlugin assemblyPlugin)
    {
        if (this.root is not null)
        {
            return;
        }

        this.root = (ModRootPluginBase)assemblyPlugin.Assembly.RootPlugin.GetInstance();
        this.assemblyPlugin = assemblyPlugin;

        string fullGuid = assemblyPlugin.Metadata.FullGuid;
        this.patcher = new Harmony(fullGuid);
        this.config = new ConfigFile(
            System.IO.Path.Combine(
                this.root.ModManager.Paths.ConfigDirectory,
                fullGuid + ".cfg"),
            false,
            assemblyPlugin.Metadata);
        this.logger = MipInEx.Logging.Logger.CreateLogSource(this.root.Name + "~" + this.Name);
    }
}


/// <summary>
/// The base class for a mod's internal plugin.
/// </summary>
/// <typeparam name="TRoot">
/// The type of the root plugin.
/// </typeparam>
public abstract class ModInternalPluginBase<TRoot> : IModInternalPluginInstanceImpl
    where TRoot : notnull, ModRootPluginBase
{
    private TRoot? root;
    private Harmony? patcher;
    private ModAssemblyInternalPlugin? assemblyPlugin;
    private ConfigFile? config;
    private ManualLogSource? logger;

    /// <inheritdoc/>
    protected ModInternalPluginBase()
    {
        this.root = null;
        this.patcher = null;
        this.assemblyPlugin = null;
        this.config = null;
        this.logger = null;
    }

    /// <inheritdoc cref="IModInternalPluginInstance.Metadata"/>
    public ModInternalPluginMetadata Metadata => this.assemblyPlugin!.Metadata;
    ModPluginMetadata IModPluginInstance.Metadata => this.assemblyPlugin!.Metadata;

    /// <inheritdoc cref="IModInternalPluginInstance.AssemblyPlugin"/>
    public ModAssemblyInternalPlugin AssemblyPlugin => this.assemblyPlugin!;
    IModAssemblyPlugin IModPluginInstance.AssemblyPlugin => this.assemblyPlugin!;

    /// <inheritdoc cref="ModAssemblyInternalPlugin.Assembly"/>
    public ModAssembly Assembly => this.assemblyPlugin!.Assembly;
    /// <inheritdoc cref="ModAssemblyInternalPlugin.ModManager"/>
    public ModManagerBase ModManager => this.assemblyPlugin!.ModManager;

    /// <inheritdoc cref="IModPluginInstance.Name"/>
    public string Name => this.assemblyPlugin!.Metadata.Name;
    /// <inheritdoc cref="IModPluginInstance.Guid"/>
    public string Guid => this.assemblyPlugin!.Metadata.Guid;
    /// <inheritdoc cref="IModPluginInstance.Version"/>
    public Version Version => this.assemblyPlugin!.Metadata.Version;

    /// <summary>
    /// The patcher for this internal plugin.
    /// </summary>
    public Harmony Patcher => this.patcher!;

    /// <summary>
    /// The config file.
    /// </summary>
    public ConfigFile Config => this.config!;

    /// <summary>
    /// The logger for this internal plugin.
    /// </summary>
    public ManualLogSource Logger => this.logger!;

    /// <inheritdoc cref="IModInternalPluginInstance.RootPlugin"/>
    public TRoot Root => this.root!;
    IModRootPluginInstance IModInternalPluginInstance.RootPlugin => this.root!;

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

    /// <inheritdoc cref="ModRootPluginBase.LoadInternalPlugin(string)"/>
    protected void LoadInternalPlugin(string guid)
    {
        this.Assembly.InternalPlugins.Load(guid);
    }

    /// <inheritdoc cref="ModRootPluginBase.UnloadInternalPlugin(string)"/>
    protected void UnloadInternalPlugin(string guid)
    {
        this.Assembly.InternalPlugins.Unload(guid);
    }

    void IModInternalPluginInstanceImpl.Initialize(ModAssemblyInternalPlugin assemblyPlugin)
    {
        if (this.root is not null)
        {
            return;
        }

        this.root = (TRoot)assemblyPlugin.Assembly.RootPlugin.GetInstance();
        this.assemblyPlugin = assemblyPlugin;

        string fullGuid = assemblyPlugin.Metadata.FullGuid;
        this.patcher = new Harmony(fullGuid);
        this.config = new ConfigFile(
            System.IO.Path.Combine(
                this.root.ModManager.Paths.ConfigDirectory,
                fullGuid + ".cfg"),
            false,
            assemblyPlugin.Metadata);
        this.logger = MipInEx.Logging.Logger.CreateLogSource(this.root.Name + "~" + this.Name);
    }
}
