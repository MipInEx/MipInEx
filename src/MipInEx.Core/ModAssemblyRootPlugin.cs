using MipInEx.Logging;
using MipInEx.Bootstrap;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MipInEx;

/// <summary>
/// A root plugin in a mod assembly.
/// </summary>
public sealed class ModAssemblyRootPlugin : IModAssemblyPlugin
{
    private readonly ModAssemblyRootPluginInfo info;
    private readonly ModAssembly assembly;
    private readonly TypeDefinitionReference typeDefinition;

    private IModRootPluginInstanceImpl? instance;
    private bool isLoaded;

    internal ModAssemblyRootPlugin(ModAssembly assembly, TypeDefinitionReference typeDefinition, ModRootPluginMetadata metadata)
    {
        this.info = new(metadata);

        this.assembly = assembly;
        this.typeDefinition = typeDefinition;
        this.instance = null;
        this.isLoaded = false;
    }

    /// <summary>
    /// The name of this root plugin.
    /// </summary>
    public string Name => this.info.Name;

    /// <summary>
    /// The guid of this root plugin.
    /// </summary>
    public string Guid => this.info.Guid;

    /// <summary>
    /// The version of this root plugin.
    /// </summary>
    public Version Version => this.info.Version;

    /// <summary>
    /// The full guid of this root plugin.
    /// </summary>
    /// <remarks>
    /// Just returns the guid of this root plugin.
    /// </remarks>
    public string FullGuid => this.info.FullGuid;

    /// <summary>
    /// Whether or not this is an internal plugin.
    /// </summary>
    /// <remarks>
    /// Always returns <c><see langword="false"/></c>.
    /// </remarks>
    public bool IsInternalPlugin => false;

    /// <summary>
    /// The mod this root plugin belongs to.
    /// </summary>
    public Mod Mod => this.assembly.Mod;

    /// <summary>
    /// The mod manager that manages the mod this root plugin
    /// belongs to.
    /// </summary>
    public ModManagerBase ModManager => this.assembly.ModManager;

    /// <summary>
    /// The assembly this root plugin belongs to.
    /// </summary>
    public ModAssembly Assembly => this.assembly;

    /// <summary>
    /// The metadata of this root plugin.
    /// </summary>
    public ModRootPluginMetadata Metadata => this.info.Metadata;
    ModPluginMetadata IModAssemblyPlugin.Metadata => this.info.Metadata;

    /// <summary>
    /// The info about this root plugin.
    /// </summary>
    public ModAssemblyRootPluginInfo Info => this.info;
    IModAssemblyPluginInfo IModAssemblyPlugin.Info => this.info;

    /// <summary>
    /// Whether or not this root plugin is loaded.
    /// </summary>
    public bool IsLoaded => this.isLoaded;

    /// <summary>
    /// Gets the descriptor string for this root plugin.
    /// </summary>
    /// <returns>
    /// The descriptor string of this root plugin.
    /// </returns>
    public string GetDescriptorString()
    {
        return $"Plugin '{this.Name}'";
    }

    /// <inheritdoc/>
    public sealed override string ToString()
    {
        return this.GetDescriptorString();
    }

    /// <summary>
    /// Gets the root plugin instance.
    /// </summary>
    /// <returns>
    /// The root plugin instance of this root plugin.
    /// </returns>
    public IModRootPluginInstance GetInstance()
    {
        if (this.instance is null)
        {
            throw new InvalidOperationException("Cannot get instance, as the root plugin hasn't been initialized!");
        }

        return this.instance;
    }

    IModPluginInstance IModAssemblyPlugin.GetInstance()
        => this.GetInstance();

    /// <summary>
    /// Loads this root plugin.
    /// </summary>
    public void Load()
    {
        if (this.isLoaded || this.instance is null)
            return;

        this.instance.Load();
        this.isLoaded = true;
        this.info.SetLoaded(true);
    }

    /// <summary>
    /// Unloads this root plugin.
    /// </summary>
    public void Unload()
    {
        if (!this.isLoaded || this.instance is null)
            return;

        try
        {
            this.instance.Unload();
        }
        catch (Exception)
        {
            this.isLoaded = false;
            this.info.SetLoaded(false);
            throw;
        }

        this.isLoaded = false;
        this.info.SetLoaded(false);
    }


    internal void Initialize(Assembly assembly)
    {
        Type type = this.typeDefinition.FetchType(assembly)!;

        try
        {
            RuntimeHelpers.RunModuleConstructor(type.Module.ModuleHandle);
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Warning, $"Couldn't run Module constructor for {assembly.FullName}::{this.typeDefinition}: {e}");
        }

        this.instance = (IModRootPluginInstanceImpl)Activator.CreateInstance(type);
        this.instance.Initialize(this);
    }
}
