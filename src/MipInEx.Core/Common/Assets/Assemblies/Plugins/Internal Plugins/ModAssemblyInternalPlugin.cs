using MipInEx.Bootstrap;
using System;
using System.Reflection;

namespace MipInEx;

/// <summary>
/// An internal plugin in a mod assembly.
/// </summary>
public sealed class ModAssemblyInternalPlugin : IModAssemblyPlugin
{
    private readonly ModAssemblyInternalPluginInfo info;
    private readonly ModAssembly assembly;
    private readonly TypeDefinitionReference typeDefinition;
    private readonly ModInternalPluginManifest manifest;

    private IModInternalPluginInstanceImpl? instance;
    private bool isLoaded;

    internal ModAssemblyInternalPlugin(ModAssembly assembly, ModInternalPluginManifest manifest, TypeDefinitionReference typeDefinition, ModInternalPluginMetadata metadata)
    {
        this.info = new(metadata);

        this.assembly = assembly;
        this.manifest = manifest;
        this.typeDefinition = typeDefinition;
        this.instance = null;
        this.isLoaded = false;
    }

    /// <summary>
    /// The name of this internal plugin.
    /// </summary>
    public string Name => this.info.Name;

    /// <summary>
    /// The guid of this internal plugin.
    /// </summary>
    public string Guid => this.info.Guid;

    /// <summary>
    /// The version of this internal plugin.
    /// </summary>
    public Version Version => this.info.Version;

    /// <summary>
    /// The name of the root plugin in the same assembly this
    /// internal plugin is defined in.
    /// </summary>
    public string RootName => this.info.RootName;

    /// <summary>
    /// The guid of the root plugin in the same assembly this
    /// internal plugin is defined in.
    /// </summary>
    public string RootGuid => this.info.RootGuid;

    /// <summary>
    /// The version of the root plugin in the same assembly
    /// this internal plugin is defined in.
    /// </summary>
    public Version RootVersion => this.info.RootVersion;

    /// <summary>
    /// The full guid of this internal plugin.
    /// </summary>
    /// <remarks>
    /// Returns the root plugin guid and this internal plugin
    /// guid seperated by a <c>~</c> character.
    /// </remarks>
    public string FullGuid => this.info.FullGuid;

    /// <summary>
    /// Whether or not this is an internal plugin.
    /// </summary>
    /// <remarks>
    /// Always returns <c><see langword="true"/></c>.
    /// </remarks>
    public bool IsInternalPlugin => true;

    /// <summary>
    /// The mod this internal plugin belongs to.
    /// </summary>
    public Mod Mod => this.assembly.Mod;

    /// <summary>
    /// The mod manager that manages the mod this internal
    /// plugin belongs to.
    /// </summary>
    public ModManagerBase ModManager => this.assembly.ModManager;

    /// <summary>
    /// The manifest of this internal plugin.
    /// </summary>
    public ModInternalPluginManifest Manifest => this.manifest;

    /// <summary>
    /// The assembly this internal plugin belongs to.
    /// </summary>
    public ModAssembly Assembly => this.assembly;

    /// <summary>
    /// The metadata of this internal plugin.
    /// </summary>
    public ModInternalPluginMetadata Metadata => this.info.Metadata;
    ModPluginMetadata IModAssemblyPlugin.Metadata => this.info.Metadata;

    /// <summary>
    /// The info about this internal plugin.
    /// </summary>
    public ModAssemblyInternalPluginInfo Info => this.info;
    IModAssemblyPluginInfo IModAssemblyPlugin.Info => this.info;

    /// <summary>
    /// Whether or not this internal plugin is loaded.
    /// </summary>
    public bool IsLoaded => this.isLoaded;

    /// <summary>
    /// Gets the descriptor string for this internal plugin.
    /// </summary>
    /// <returns>
    /// The descriptor string of this internal plugin.
    /// </returns>
    public string GetDescriptorString()
    {
        return $"Internal Plugin '{this.Name}'";
    }

    /// <inheritdoc/>
    public sealed override string ToString()
    {
        return this.GetDescriptorString();
    }

    /// <summary>
    /// Gets the internal plugin instance.
    /// </summary>
    /// <returns>
    /// The internal plugin instance of this internal plugin.
    /// </returns>
    public IModInternalPluginInstance GetInstance()
    {
        if (this.instance is null)
        {
            throw new InvalidOperationException("Cannot get instance, as the internal plugin hasn't been initialized!");
        }

        return this.instance;
    }

    IModPluginInstance IModAssemblyPlugin.GetInstance()
        => this.GetInstance();

    /// <summary>
    /// Loads this internal plugin.
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
    /// Unloads this internal plugin.
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
        this.instance = (IModInternalPluginInstanceImpl)Activator.CreateInstance(type);
        this.instance.Initialize(this);
    }
}
