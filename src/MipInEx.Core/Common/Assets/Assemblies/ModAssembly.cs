using MipInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Collections.Frozen;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MipInEx.Logging;

namespace MipInEx;

/// <summary>
/// A mod assembly.
/// </summary>
public sealed class ModAssembly : IModAsset
{
    private readonly ModAssemblyInfo info;
    private readonly Mod mod;
    private readonly ModAssemblyImporter importer;
    private readonly ModAssemblyManifest manifest;

    private Assembly? instance;

    private readonly ModAssemblyRootPlugin rootPlugin;
    private readonly InternalPluginCollection internalPlugins;

    private LoadAsyncOperation? loadOperation;
    private UnloadAsyncOperation? unloadOperation;

    internal ModAssembly(
        Mod mod,
        ModAssemblyManifest manifest,
        ModAssemblyImporter importer,
        PluginReference rootPluginReference,
        IEnumerable<PluginReference> internalPluginReferences)
    {
        this.mod = mod;
        this.manifest = manifest;
        this.importer = importer;
        this.rootPlugin = new ModAssemblyRootPlugin(
            this,
            rootPluginReference.Type,
            new ModRootPluginMetadata(
                rootPluginReference.Guid,
                rootPluginReference.Name,
                rootPluginReference.Version));

        List<ModAssemblyInternalPlugin> internalPlugins = new();

        foreach (PluginReference internalPluginReference in internalPluginReferences)
        {
            string guid = internalPluginReference.Guid;

            ModInternalPluginManifest? foundManifest = null;
            foreach (ModInternalPluginManifest internalPluginManifest in this.manifest.InternalPlugins)
            {
                if (internalPluginManifest.Guid == guid)
                {
                    foundManifest = internalPluginManifest;
                    break;
                }
            }

            if (foundManifest == null)
            {
                // create default manifest
                foundManifest = new ModInternalPluginManifest(guid, 0, false);
            }

            internalPlugins.Add(new ModAssemblyInternalPlugin(
                this,
                foundManifest,
                internalPluginReference.Type,
                new ModInternalPluginMetadata(
                    this.rootPlugin.Metadata,
                    guid,
                    internalPluginReference.Name,
                    internalPluginReference.Version)));
        }

        internalPlugins.Sort((a, b) => b.Manifest.LoadPriority.CompareTo(a.Manifest.LoadPriority));

        this.internalPlugins = new InternalPluginCollection(internalPlugins.ToImmutableArray());

        this.info = new(
            importer.Name,
            Utility.ShortenAssetPath(
                importer.FullAssetPath, 
                "Assemblies",
                StringComparison.OrdinalIgnoreCase),
            importer.FullAssetPath,
            this.rootPlugin.Info,
            internalPlugins.Select(x => x.Info).ToImmutableArray());
    }

    internal ModAssemblyImporter Importer => this.importer;

    /// <summary>
    /// The root plugin in this assembly.
    /// </summary>
    public ModAssemblyRootPlugin RootPlugin => this.rootPlugin;

    /// <summary>
    /// The internal plugins in this assembly.
    /// </summary>
    public InternalPluginCollection InternalPlugins => this.internalPlugins;

    /// <summary>
    /// The mod this assembly belongs to.
    /// </summary>
    public Mod Mod => this.mod;

    /// <summary>
    /// The mod manager that manages the mod this assembly
    /// belongs to.
    /// </summary>
    public ModManagerBase ModManager => this.mod.ModManager;

    /// <summary>
    /// The manifest of this assembly.
    /// </summary>
    public ModAssemblyManifest Manifest => this.manifest;
    IModAssetManifest IModAsset.Manifest => this.manifest;

    /// <summary>
    /// The info about this assembly.
    /// </summary>
    public ModAssemblyInfo Info => this.info;
    IModAssetInfo IModAsset.Info => this.info;

    /// <summary>
    /// The name of the assembly.
    /// </summary>
    public string Name => this.info.Name;

    /// <summary>
    /// The path of the assembly asset. 
    /// </summary>
    public string AssetPath => this.info.AssetPath;

    /// <summary>
    /// The full asset path of the assembly asset.
    /// </summary>
    public string FullAssetPath => this.info.FullAssetPath;

    string IModAsset.AssetPath => this.info.AssetPath;
    string IModAsset.FullAssetPath => this.info.FullAssetPath;

    /// <summary>
    /// The state of this assembly.
    /// </summary>
    public ModAssetState State => this.info.State;

    /// <summary>
    /// Whether or not this assembly is loaded.
    /// </summary>
    public bool IsLoaded => this.instance != null && this.info.IsLoaded;

    /// <summary>
    /// Whether or not this assembly is unloaded.
    /// </summary>
    public bool IsUnloaded => this.instance == null && this.info.IsUnloaded;

    /// <summary>
    /// The type of the mod asset.
    /// </summary>
    /// <remarks>
    /// Will always return
    /// <see cref="ModAssetType.Assembly"/>.
    /// </remarks>
    public ModAssetType Type => ModAssetType.Assembly;

    /// <summary>
    /// Gets the descriptor string for this assembly.
    /// </summary>
    /// <returns>
    /// The descriptor string of this assembly.
    /// </returns>
    public string GetDescriptorString()
    {
        return $"Assembly '{this.Name}'";
    }

    /// <inheritdoc/>
    public sealed override string ToString()
    {
        return this.GetDescriptorString();
    }

    /// <summary>
    /// Loads the assembly synchronously.
    /// </summary>
    /// <remarks>
    /// Will throw an <see cref="InvalidOperationException"/>
    /// in the following asset states:
    /// <list type="bullet">
    /// <item>
    /// <see cref="ModAssetState.Loading"/>
    /// </item>
    /// <item>
    /// <see cref="ModAssetState.Unloading"/>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the assembly is unloading asynchronously or
    /// an asynchronous load request is active.
    /// </exception>
    public void Load()
    {
        if (this.IsLoaded)
            return;
        else if (this.unloadOperation != null)
            throw new InvalidOperationException("Cannot load whilst the assembly is being unloaded!");
        else if (this.loadOperation != null)
            throw new InvalidOperationException("An existing async load request is already active!");

        this.info.SetState(ModAssetState.Loading);
        try
        {
            if (this.instance == null)
            {
                this.instance = this.importer.Import();
            }

            if (!this.rootPlugin.IsLoaded)
            {
                this.rootPlugin.Load();
            }

            foreach (ModAssemblyInternalPlugin internalPlugin in this.internalPlugins)
            {
                if (internalPlugin.IsLoaded || internalPlugin.Manifest.LoadManually)
                    continue;

                internalPlugin.Load();
            }
        }
        catch (Exception)
        {
            this.info.SetState(ModAssetState.Loaded);
            throw;
        }

        this.info.SetState(ModAssetState.Loaded);
    }


    /// <summary>
    /// Loads the assembly asynchronously.
    /// </summary>
    /// <remarks>
    /// Will return a faulted <see cref="ModAsyncOperation"/>
    /// with an <see cref="InvalidOperationException"/>
    /// exception in the following asset states:
    /// <list type="bullet">
    /// <item>
    /// <see cref="ModAssetState.Unloading"/>
    /// </item>
    /// </list>
    /// <para>
    /// If an asynchronous load request is active, then this
    /// method will return the existing request.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The load <see cref="ModAsyncOperation"/>, 
    /// <see cref="ModAsyncOperation.Completed"/> if the
    /// assembly is already loaded, or a faulted
    /// <see cref="ModAsyncOperation"/> with an exception
    /// of type <see cref="InvalidOperationException"/> if the
    /// assembly is being unloaded asynchronously.
    /// </returns>
    public ModAsyncOperation LoadAsync()
    {
        if (this.IsLoaded)
            return ModAsyncOperation.Completed;
        else if (this.unloadOperation != null)
            return ModAsyncOperation.FromException(new InvalidOperationException("Cannot load whilst the assembly is being unloaded!"));

        this.loadOperation ??= new LoadAsyncOperation(this);
        return this.loadOperation;
    }

    /// <summary>
    /// Unloads the assembly synchronously.
    /// </summary>
    /// <remarks>
    /// Will throw an <see cref="InvalidOperationException"/>
    /// in the following asset states:
    /// <list type="bullet">
    /// <item>
    /// <see cref="ModAssetState.Loading"/>
    /// </item>
    /// <item>
    /// <see cref="ModAssetState.Unloading"/>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the assembly is loading asynchronously or an
    /// asynchronous unload request is active.
    /// </exception>
    public void Unload()
    {
        if (this.IsUnloaded)
            return;
        else if (this.loadOperation != null)
            throw new InvalidOperationException("Cannot unload whilst the assembly is being loaded!");
        else if (this.unloadOperation != null)
            throw new InvalidOperationException("An existing async unload request is already active!");

        this.info.SetState(ModAssetState.Unloading);
        for (int index = this.internalPlugins.Count - 1; index >= 0; index--)
        {
            ModAssemblyInternalPlugin internalPlugin = this.internalPlugins[index];
            if (!internalPlugin.IsLoaded)
                continue;

            try
            {
                internalPlugin.Unload();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error whilst unloading {internalPlugin} in {this} in {this.mod}: {ex}");
            }
        }

        if (this.rootPlugin.IsLoaded)
        {
            try
            {
                this.rootPlugin.Unload();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error whilst unloading {this.rootPlugin} in {this} in {this.mod}: {ex}");
            }
        }

        if (this.instance != null)
        {
            this.instance = null;
        }

        this.info.SetState(ModAssetState.Unloaded);
    }

    /// <summary>
    /// Unloads the assembly asynchronously.
    /// </summary>
    /// <remarks>
    /// Will return a faulted <see cref="ModAsyncOperation"/>
    /// with an <see cref="InvalidOperationException"/>
    /// exception in the following asset states:
    /// <list type="bullet">
    /// <item>
    /// <see cref="ModAssetState.Loading"/>
    /// </item>
    /// </list>
    /// <para>
    /// If an asynchronous unload request is active, then this
    /// method will return the existing request.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the mod asset is loading asynchronously.
    /// </exception>
    /// <returns>
    /// The unload <see cref="ModAsyncOperation"/>,
    /// <see cref="ModAsyncOperation.Completed"/> if the
    /// assembly is already unloaded, or a faulted
    /// <see cref="ModAsyncOperation"/> with an exception
    /// of type <see cref="InvalidOperationException"/> if the
    /// assembly is being loaded asynchronously.
    /// </returns>
    public ModAsyncOperation UnloadAsync()
    {
        if (this.IsUnloaded)
            return ModAsyncOperation.Completed;
        else if (this.loadOperation != null)
            return ModAsyncOperation.FromException(new InvalidOperationException("Cannot unload whilst the assembly is being loaded!"));

        this.unloadOperation ??= new UnloadAsyncOperation(this);
        return this.unloadOperation;
    }

    private sealed class LoadAssemblyAsyncOperation : ModAsyncOperation
    {
        private readonly ModAssembly modAssembly;
        private bool isDone;
        private Exception? exception;

        internal LoadAssemblyAsyncOperation(ModAssembly modAssembly)
        {
            this.modAssembly = modAssembly;
            this.isDone = false;
            this.exception = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone)
                {
                    if (this.exception is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                return ModAsyncOperationStatus.NotStarted;
            }
        }
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone && this.exception is null;
        public sealed override bool IsFaulted => this.isDone && this.exception is not null;
        public sealed override Exception? Exception => this.exception;

        public sealed override string? GetDescriptionString()
        {
            if (this.isDone)
                return null;
            else
                return "Loading assembly...";
        }

        public sealed override double GetProgress()
        {
            return this.isDone ? 1.0 : 0.0;
        }

        public sealed override bool Process()
        {
            if (this.isDone)
            {
                return true;
            }

            if (this.modAssembly.instance == null)
            {
                Assembly assembly;
                try
                {
                    assembly = this.modAssembly.importer.Import();
                }
                catch (Exception ex)
                {
                    this.exception = ex;
                    this.isDone = true;
                    return true;
                }

                this.modAssembly.instance = assembly;
            }

            this.isDone = true;
            this.exception = null;
            return true;
        }
    }

    private sealed class UnloadAssemblyAsyncOperation : ModAsyncOperation
    {
        private readonly ModAssembly modAssembly;
        private bool isDone;

        internal UnloadAssemblyAsyncOperation(ModAssembly modAssembly)
        {
            this.modAssembly = modAssembly;
            this.isDone = false;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone)
                    return ModAsyncOperationStatus.SuccessComplete;
                else
                    return ModAsyncOperationStatus.NotStarted;
            }
        }
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone;
        public sealed override bool IsFaulted => false;
        public sealed override Exception? Exception => null;

        public sealed override string? GetDescriptionString()
        {
            if (this.isDone)
                return null;
            else
                return "Unloading assembly...";
        }

        public sealed override double GetProgress()
        {
            return this.isDone ? 1.0 : 0.0;
        }

        public sealed override bool Process()
        {
            if (this.isDone)
            {
                return true;
            }

            if (this.modAssembly.instance != null)
            {
                this.modAssembly.instance = null;
            }

            this.isDone = true;
            return true;
        }
    }

    private sealed class LoadRootPluginAsyncOperation : ModAsyncOperation
    {
        private readonly ModAssemblyRootPlugin rootPlugin;
        private bool isDone;
        private Exception? exception;

        internal LoadRootPluginAsyncOperation(ModAssemblyRootPlugin rootPlugin)
        {
            this.rootPlugin = rootPlugin;
            this.isDone = false;
            this.exception = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone)
                {
                    if (this.exception is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                return ModAsyncOperationStatus.NotStarted;
            }
        }
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone && this.exception is null;
        public sealed override bool IsFaulted => this.isDone && this.exception is not null;
        public sealed override Exception? Exception => this.exception;

        public sealed override string? GetDescriptionString()
        {
            if (this.isDone)
                return null;
            else
                return $"Loading root plugin {this.rootPlugin.Name}...";
        }

        public sealed override double GetProgress()
        {
            return this.isDone ? 1.0 : 0.0;
        }

        public sealed override bool Process()
        {
            if (this.isDone)
            {
                return true;
            }

            try
            {
                this.rootPlugin.Load();
            }
            catch (Exception ex)
            {
                this.exception = ex;
            }

            this.isDone = true;
            return true;
        }
    }

    private sealed class UnloadRootPluginAsyncOperation : ModAsyncOperation
    {
        private readonly ModAssemblyRootPlugin rootPlugin;
        private bool isDone;
        private Exception? exception;

        internal UnloadRootPluginAsyncOperation(ModAssemblyRootPlugin rootPlugin)
        {
            this.rootPlugin = rootPlugin;
            this.isDone = false;
            this.exception = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone)
                {
                    if (this.exception is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                return ModAsyncOperationStatus.NotStarted;
            }
        }
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone && this.exception is null;
        public sealed override bool IsFaulted => this.isDone && this.exception is not null;
        public sealed override Exception? Exception => this.exception;

        public sealed override string? GetDescriptionString()
        {
            if (this.isDone)
                return null;
            else
                return $"Unloading root plugin {this.rootPlugin.Name}...";
        }

        public sealed override double GetProgress()
        {
            return this.isDone ? 1.0 : 0.0;
        }

        public sealed override bool Process()
        {
            if (this.isDone)
            {
                return true;
            }

            try
            {
                this.rootPlugin.Unload();
            }
            catch (Exception ex)
            {
                this.exception = ex;
            }

            this.isDone = true;
            return true;
        }
    }
    
    private sealed class LoadInternalPluginAsyncOperation : ModAsyncOperation
    {
        private readonly ModAssemblyInternalPlugin internalPlugin;
        private bool isDone;
        private Exception? exception;

        internal LoadInternalPluginAsyncOperation(ModAssemblyInternalPlugin internalPlugin)
        {
            this.internalPlugin = internalPlugin;
            this.isDone = false;
            this.exception = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone)
                {
                    if (this.exception is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                return ModAsyncOperationStatus.NotStarted;
            }
        }
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone && this.exception is null;
        public sealed override bool IsFaulted => this.isDone && this.exception is not null;
        public sealed override Exception? Exception => this.exception;

        public sealed override double GetProgress()
        {
            return this.isDone ? 1.0 : 0.0;
        }

        public sealed override string? GetDescriptionString()
        {
            if (this.isDone)
                return null;
            else
                return $"Loading internal plugin {this.internalPlugin.Name}...";
        }

        public sealed override bool Process()
        {
            if (this.isDone)
            {
                return true;
            }

            try
            {
                this.internalPlugin.Load();
            }
            catch (Exception ex)
            {
                this.exception = ex;
            }

            this.isDone = true;
            return true;
        }
    }

    private sealed class UnloadInternalPluginAsyncOperation : ModAsyncOperation
    {
        private readonly ModAssemblyInternalPlugin internalPlugin;
        private bool isDone;
        private Exception? exception;

        internal UnloadInternalPluginAsyncOperation(ModAssemblyInternalPlugin internalPlugin)
        {
            this.internalPlugin = internalPlugin;
            this.isDone = false;
            this.exception = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone)
                {
                    if (this.exception is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                return ModAsyncOperationStatus.NotStarted;
            }
        }
        public sealed override bool IsRunning => false;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone && this.exception is null;
        public sealed override bool IsFaulted => this.isDone && this.exception is not null;
        public sealed override Exception? Exception => this.exception;

        public sealed override string? GetDescriptionString()
        {
            if (this.isDone)
                return null;
            else
                return $"Unloading internal plugin {this.internalPlugin.Name}...";
        }

        public sealed override double GetProgress()
        {
            return this.isDone ? 1.0 : 0.0;
        }

        public sealed override bool Process()
        {
            if (this.isDone)
            {
                return true;
            }

            try
            {
                this.internalPlugin.Unload();
            }
            catch (Exception ex)
            {
                this.exception = ex;
            }

            this.isDone = true;
            return true;
        }
    }

    private sealed class LoadAsyncOperation : ModAsyncMultiOperation
    {
        private readonly ModAssembly modAssembly;
        private readonly ModAsyncOperation[] asyncOperations;
        private ModAsyncOperation? currentOperation;
        private int index;
        private Exception? exception;

        internal LoadAsyncOperation(ModAssembly modAssembly)
        {
            this.modAssembly = modAssembly;
            List<ModAsyncOperation> asyncOperations = new();

            if (modAssembly.instance == null)
            {
                asyncOperations.Add(new LoadAssemblyAsyncOperation(modAssembly));
            }

            if (!modAssembly.rootPlugin.IsLoaded)
            {
                asyncOperations.Add(new LoadRootPluginAsyncOperation(modAssembly.rootPlugin));
            }

            foreach (ModAssemblyInternalPlugin internalPlugin in modAssembly.internalPlugins)
            {
                if (internalPlugin.IsLoaded || internalPlugin.Manifest.LoadManually)
                    continue;

                asyncOperations.Add(new LoadInternalPluginAsyncOperation(internalPlugin));
            }

            this.index = -1;
            this.currentOperation = null;
            this.asyncOperations = asyncOperations.ToArray();
            this.exception = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.index == -1)
                    return ModAsyncOperationStatus.NotStarted;
                else if (this.index >= this.asyncOperations.Length)
                {
                    if (this.exception is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                else
                    return ModAsyncOperationStatus.Running;
            }
        }
        public sealed override bool IsRunning => this.index > -1 && this.index < this.asyncOperations.Length;
        public sealed override bool IsCompleted => this.index >= this.asyncOperations.Length;
        public sealed override bool IsCompletedSuccessfully => this.index >= this.asyncOperations.Length && this.exception is null;
        public sealed override bool IsFaulted => this.index >= this.asyncOperations.Length && this.exception is not null;
        public sealed override Exception? Exception => this.exception;

        public sealed override int OperationCount
            => this.asyncOperations.Length;
        public sealed override int CompletedOperationCount
            => Math.Clamp(0, this.index, this.asyncOperations.Length);

        public sealed override double GetTotalProgress()
        {
            if (this.index < 0)
                return 0.0;
            else if (this.asyncOperations.Length == 0)
                return 1.0;
            else
                return this.index + (this.currentOperation?.GetProgress() ?? 0.0);
        }

        public sealed override string? GetDescriptionString()
        {
            return this.currentOperation?.GetDescriptionString();
        }

        public sealed override double GetProgress()
        {
            if (this.index < 0)
                return 0.0;
            else if (this.asyncOperations.Length == 0)
                return 1.0;
            else
                return (this.index + (this.currentOperation?.GetProgress() ?? 0.0)) / this.asyncOperations.Length;
        }

        public sealed override bool Process()
        {
            if (this.index >= this.asyncOperations.Length)
            {
                return true;
            }

            if (this.index < 0)
            {
                this.modAssembly.info.SetState(ModAssetState.Loading);
                this.index++;
                if (this.asyncOperations.Length == 0)
                {
                    this.modAssembly.info.SetState(ModAssetState.Loaded);
                    this.modAssembly.loadOperation = null;
                    return true;
                }
            }

            if (this.currentOperation == null)
            {
                this.currentOperation = this.asyncOperations[this.index];
                return false;
            }

            if (!this.currentOperation.Process())
            {
                return false;
            }

            if (this.currentOperation.IsFaulted)
            {
                this.exception = this.currentOperation.Exception;
                this.index = this.asyncOperations.Length;
                this.currentOperation = null;
                this.modAssembly.info.SetState(ModAssetState.Loaded);
                this.modAssembly.loadOperation = null;
                return true;
            }

            this.currentOperation = null;
            this.index++;

            if (this.index >= this.asyncOperations.Length)
            {
                this.modAssembly.info.SetState(ModAssetState.Loaded);
                this.modAssembly.loadOperation = null;
                return true;
            }

            this.currentOperation = this.asyncOperations[this.index];
            return false;
        }
    }

    private sealed class UnloadAsyncOperation : ModAsyncMultiOperation
    {
        private readonly ModAssembly modAssembly;
        private readonly ModAsyncOperation[] asyncOperations;
        private readonly List<Exception> exceptions;
        private ModAsyncOperation? currentOperation;
        private int index;
        private Exception? rootException;

        internal UnloadAsyncOperation(ModAssembly modAssembly)
        {
            this.modAssembly = modAssembly;
            this.exceptions = new();
            List<ModAsyncOperation> asyncOperations = new();

            for (int index = modAssembly.internalPlugins.Count - 1; index >= 0; index--)
            {
                ModAssemblyInternalPlugin internalPlugin = modAssembly.internalPlugins[index];
                if (!internalPlugin.IsLoaded)
                {
                    continue;
                }

                asyncOperations.Add(new UnloadInternalPluginAsyncOperation(internalPlugin));
            }

            if (modAssembly.rootPlugin.IsLoaded)
            {
                asyncOperations.Add(new UnloadRootPluginAsyncOperation(modAssembly.rootPlugin));
            }

            if (modAssembly.instance != null)
            {
                asyncOperations.Add(new UnloadAssemblyAsyncOperation(modAssembly));
            }

            this.index = -1;
            this.currentOperation = null;
            this.asyncOperations = asyncOperations.ToArray();
            this.rootException = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.index == -1)
                    return ModAsyncOperationStatus.NotStarted;
                else if (this.index >= this.asyncOperations.Length)
                {
                    if (this.rootException is null)
                        return ModAsyncOperationStatus.SuccessComplete;
                    else
                        return ModAsyncOperationStatus.FaultComplete;
                }
                else
                    return ModAsyncOperationStatus.Running;
            }
        }
        public sealed override bool IsRunning => this.index > -1 && this.index < this.asyncOperations.Length;
        public sealed override bool IsCompleted => this.index >= this.asyncOperations.Length;
        public sealed override bool IsCompletedSuccessfully => this.index >= this.asyncOperations.Length && this.rootException is null;
        public sealed override bool IsFaulted => this.index >= this.asyncOperations.Length && this.rootException is not null;
        public sealed override Exception? Exception => this.rootException;

        public sealed override int OperationCount
            => this.asyncOperations.Length;
        public sealed override int CompletedOperationCount
            => Math.Clamp(0, this.index, this.asyncOperations.Length);

        public sealed override double GetTotalProgress()
        {
            if (this.index < 0)
                return 0.0;
            else if (this.asyncOperations.Length == 0)
                return 1.0;
            else
                return this.index + (this.currentOperation?.GetProgress() ?? 0.0);
        }

        public sealed override string? GetDescriptionString()
        {
            return this.currentOperation?.GetDescriptionString();
        }

        public sealed override double GetProgress()
        {
            if (this.index < 0)
                return 0.0;
            else if (this.asyncOperations.Length == 0)
                return 1.0;
            else
                return (this.index + (this.currentOperation?.GetProgress() ?? 0.0)) / this.asyncOperations.Length;
        }

        public sealed override bool Process()
        {
            if (this.index >= this.asyncOperations.Length)
            {
                return true;
            }

            if (this.index < 0)
            {
                this.modAssembly.info.SetState(ModAssetState.Unloading);
                this.index++;
                if (this.asyncOperations.Length == 0)
                {
                    this.modAssembly.info.SetState(ModAssetState.Unloaded);
                    this.modAssembly.unloadOperation = null;
                    return true;
                }
            }

            if (this.currentOperation == null)
            {
                this.currentOperation = this.asyncOperations[this.index];
                return false;
            }

            if (!this.currentOperation.Process())
            {
                return false;
            }

            if (this.currentOperation.IsFaulted)
            {
                Exception? ex = this.currentOperation.Exception;
                if (ex != null)
                {
                    this.exceptions.Add(ex);
                }
            }

            this.currentOperation = null;
            this.index++;

            if (this.index < this.asyncOperations.Length)
            {
                this.currentOperation = this.asyncOperations[this.index];
                return false;
            }

            if (this.exceptions.Count > 0)
            {
                this.rootException = new AggregateException("Errors occurred whilst unloading the assembly", this.exceptions);
            }
            this.modAssembly.unloadOperation = null;
            this.modAssembly.info.SetState(ModAssetState.Unloaded);
            return true;

        }
    }

    /// <summary>
    /// A collection of
    /// <see cref="ModAssemblyInternalPlugin"/>s.
    /// </summary>
    public sealed class InternalPluginCollection : 
        IReadOnlyList<ModAssemblyInternalPlugin>,
        IReadOnlyCollection<ModAssemblyInternalPlugin>,

        IList<ModAssemblyInternalPlugin>,
        ICollection<ModAssemblyInternalPlugin>,

        IList,
        ICollection,

        IEnumerable<ModAssemblyInternalPlugin>,
        IEnumerable,

        IEquatable<InternalPluginCollection>
    {
        private readonly ImmutableArray<ModAssemblyInternalPlugin> internalPlugins;
        private readonly FrozenDictionary<string, ModAssemblyInternalPlugin> guidToInternalPluginDictionary;

        internal InternalPluginCollection(ImmutableArray<ModAssemblyInternalPlugin> internalPlugins)
        {
            this.internalPlugins = internalPlugins;
            if (internalPlugins.IsEmpty)
            {
                this.guidToInternalPluginDictionary = FrozenDictionary<string, ModAssemblyInternalPlugin>.Empty;
            }
            else
            {
                this.guidToInternalPluginDictionary = this.internalPlugins.ToFrozenDictionary(
                    keySelector: (internalPlugin) => internalPlugin.Metadata.Guid);
            }
        }

        /// <summary>
        /// The number of internal plugins in this collection.
        /// </summary>
        public int Count => this.internalPlugins.Length;

        /// <summary>
        /// The guids of the internal plugins in this
        /// collection.
        /// </summary>
        /// <remarks>
        /// Note: The index of a guid in this collection won't
        /// be a 1:1 mapping of the internal plugin at that
        /// index.
        /// </remarks>
        public IReadOnlyList<string> Guids => this.guidToInternalPluginDictionary.Keys;

        bool ICollection<ModAssemblyInternalPlugin>.IsReadOnly => true;
        bool IList.IsReadOnly => true;
        bool IList.IsFixedSize => true;
        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => this;

        /// <summary>
        /// Gets the mod assembly internal plugin at the
        /// specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the mod assembly internal
        /// plugin to get.
        /// </param>
        /// <returns>
        /// The mod assembly internal plugin at the specified
        /// index.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException">
        /// <paramref name="index"/> is out of range.
        /// </exception>
        public ModAssemblyInternalPlugin this[int index]
            => this.internalPlugins[index];

        /// <summary>
        /// Gets the mod assembly internal plugin with the
        /// specified <paramref name="guid"/>.
        /// </summary>
        /// <param name="guid">
        /// The guid of the mod assembly internal plugin to
        /// fetch.
        /// </param>
        /// <returns>
        /// The mod assembly internal plugin with the specified
        /// <paramref name="guid"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="guid"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// No mod assembly internal plugin with guid
        /// <paramref name="guid"/> was found.
        /// </exception>
        public ModAssemblyInternalPlugin this[string guid]
        {
            get
            {
                if (guid is null)
                    throw new ArgumentNullException(nameof(guid));

                return this.guidToInternalPluginDictionary[guid];
            }
        }

        ModAssemblyInternalPlugin IList<ModAssemblyInternalPlugin>.this[int index]
        {
            get => this.internalPlugins[index];
            set => throw new NotSupportedException();
        }

        object IList.this[int index]
        {
            get => this.internalPlugins[index];
            set => throw new NotSupportedException();
        }

        void ICollection<ModAssemblyInternalPlugin>.Add(ModAssemblyInternalPlugin item)
            => throw new NotSupportedException();

        int IList.Add(object value)
            => throw new NotSupportedException();

        void ICollection<ModAssemblyInternalPlugin>.Clear()
            => throw new NotSupportedException();

        void IList.Clear()
            => throw new NotSupportedException();

        bool ICollection<ModAssemblyInternalPlugin>.Contains(ModAssemblyInternalPlugin item)
            => this.ContainsInternalPlugin(item);

        /// <summary>
        /// Returns whether or not this collection contains an
        /// internal plugin with the specified guid.
        /// </summary>
        /// <param name="guid">
        /// The guid of the internal plugin.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an internal plugin with
        /// guid <paramref name="guid"/> exists; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool ContainsGuid([NotNullWhen(true)] string? guid)
        {
            return guid is not null && this.guidToInternalPluginDictionary.ContainsKey(guid);
        }

        /// <summary>
        /// Returns whether or not this collection contains the
        /// specified mod assembly internal plugin.
        /// </summary>
        /// <param name="internalPlugin">
        /// The mod assembly internal plugin to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this collection contains
        /// <paramref name="internalPlugin"/>; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool ContainsInternalPlugin([NotNullWhen(true)] ModAssemblyInternalPlugin? internalPlugin)
        {
            return internalPlugin is not null && this.internalPlugins.Contains(internalPlugin);
        }

        /// <summary>
        /// Returns whether or not this collection contains the
        /// specified mod assembly internal plugin optionally
        /// using an equality comparer.
        /// </summary>
        /// <param name="internalPlugin">
        /// The mod assembly internal plugin to check.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to use.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this collection contains
        /// <paramref name="internalPlugin"/>; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool ContainsInternalPlugin([NotNullWhen(true)] ModAssemblyInternalPlugin? internalPlugin, IEqualityComparer<ModAssemblyInternalPlugin>? equalityComparer)
        {
            return internalPlugin is not null && this.internalPlugins.Contains(internalPlugin, equalityComparer);
        }

        bool IList.Contains(object? value)
        {
            return this.ContainsInternalPlugin(value as ModAssemblyInternalPlugin);
        }

        /// <summary>
        /// Copies the current elements of this collection to
        /// the specified span.
        /// </summary>
        /// <param name="destination">
        /// The span to copy to.
        /// </param>
        public void CopyTo(Span<ModAssemblyInternalPlugin> destination)
        {
            this.internalPlugins.CopyTo(destination);
        }

        /// <summary>
        /// Copies the current elements of this collection to
        /// the specified array.
        /// </summary>
        /// <param name="destination">
        /// The array to copy to.
        /// </param>
        public void CopyTo(ModAssemblyInternalPlugin[] destination)
        {
            this.internalPlugins.CopyTo(destination);
        }

        /// <summary>
        /// Copies the current elements of this collection to
        /// the specified array starting at the specified
        /// destination index.
        /// </summary>
        /// <param name="destination">
        /// The array to copy to.
        /// </param>
        /// <param name="destinationIndex">
        /// The zero-based index in
        /// <paramref name="destination"/> where copying
        /// begins.
        /// </param>
        public void CopyTo(ModAssemblyInternalPlugin[] destination, int destinationIndex)
        {
            this.internalPlugins.CopyTo(destination, destinationIndex);
        }

        /// <summary>
        /// Copies the current elements of this collection to
        /// the specified array starting at the specified
        /// starting index.
        /// </summary>
        /// <param name="sourceIndex">
        /// The zero-based index in this collection
        /// where copying beings.
        /// </param>
        /// <param name="destination">
        /// The array to copy to.
        /// </param>
        /// <param name="destinationIndex">
        /// The zero-based index in
        /// <paramref name="destination"/> where copying
        /// begins.
        /// </param>
        /// <param name="length">
        /// The number of elements to copy from this collection.
        /// </param>
        public void CopyTo(int sourceIndex, ModAssemblyInternalPlugin[] destination, int destinationIndex, int length)
        {
            this.internalPlugins.CopyTo(sourceIndex, destination, destinationIndex, length);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.internalPlugins).CopyTo(array, index);
        }

        /// <inheritdoc/>
        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null)
                return false;
            else if (obj is InternalPluginCollection internalPluginCollection)
                return this.internalPlugins.Equals(internalPluginCollection.internalPlugins);
            else
                return false;
        }

        /// <summary>
        /// Returns whether or not this collection equals the
        /// specified internal plugin collection.
        /// </summary>
        /// <param name="other">
        /// The other collection.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the collections are
        /// equal; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals([NotNullWhen(true)] InternalPluginCollection? other)
        {
            if (other is null)
                return false;
            else
                return this.internalPlugins.Equals(other.internalPlugins);
        }

        /// <summary>
        /// Gets an enumerator to enumerate through this
        /// collection.
        /// </summary>
        /// <returns>
        /// An enumerator to enumerate through this collection.
        /// </returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<ModAssemblyInternalPlugin> IEnumerable<ModAssemblyInternalPlugin>.GetEnumerator()
        {
            return ((IEnumerable<ModAssemblyInternalPlugin>)this.internalPlugins).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.internalPlugins).GetEnumerator();
        }

        /// <inheritdoc/>
        public sealed override int GetHashCode()
        {
            return this.internalPlugins.GetHashCode();
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int IndexOf(ModAssemblyInternalPlugin? internalPlugin)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index to begin the search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int IndexOf(ModAssemblyInternalPlugin? internalPlugin, int startIndex)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, startIndex);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index to begin the search.
        /// </param>
        /// <param name="count">
        /// The number of internal plugins to search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int IndexOf(ModAssemblyInternalPlugin? internalPlugin, int startIndex, int count)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, startIndex, count);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int IndexOf(ModAssemblyInternalPlugin? internalPlugin, IEqualityComparer<ModAssemblyInternalPlugin>? equalityComparer)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, 0, this.Count, equalityComparer);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index to begin the search.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int IndexOf(ModAssemblyInternalPlugin? internalPlugin, int startIndex, IEqualityComparer<ModAssemblyInternalPlugin>? equalityComparer)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, startIndex, equalityComparer);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index to begin the search.
        /// </param>
        /// <param name="count">
        /// The number of internal plugins to search.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int IndexOf(ModAssemblyInternalPlugin? internalPlugin, int startIndex, int count, IEqualityComparer<ModAssemblyInternalPlugin>? equalityComparer)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, startIndex, count, equalityComparer);
        }

        int IList.IndexOf(object? value)
        {
            if (value is not ModAssemblyInternalPlugin internalPlugin)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, 0, this.Count, EqualityComparer<ModAssemblyInternalPlugin>.Default);
        }

        void IList<ModAssemblyInternalPlugin>.Insert(int index, ModAssemblyInternalPlugin item)
            => throw new NotSupportedException();

        void IList.Insert(int index, object? value)
            => throw new NotSupportedException();

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin; starting at the end of the collection.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int LastIndexOf(ModAssemblyInternalPlugin? internalPlugin)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin; starting at the end of the collection.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index to begin the search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int LastIndexOf(ModAssemblyInternalPlugin? internalPlugin, int startIndex)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, startIndex);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin; starting at the end of the collection.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index to begin the search.
        /// </param>
        /// <param name="count">
        /// The number of internal plugins to search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int LastIndexOf(ModAssemblyInternalPlugin? internalPlugin, int startIndex, int count)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, startIndex, count);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin; starting at the end of the collection.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int LastIndexOf(ModAssemblyInternalPlugin? internalPlugin, IEqualityComparer<ModAssemblyInternalPlugin>? equalityComparer)
        {
            if (internalPlugin is null || this.internalPlugins.IsEmpty)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, this.Count - 1, this.Count, equalityComparer);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin; starting at the end of the collection.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index to begin the search.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int LastIndexOf(ModAssemblyInternalPlugin? internalPlugin, int startIndex, IEqualityComparer<ModAssemblyInternalPlugin>? equalityComparer)
        {
            if (internalPlugin is null || (this.internalPlugins.IsEmpty && startIndex == 0))
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, startIndex, startIndex + 1, equalityComparer);
        }

        /// <summary>
        /// Searches this collection for the specified internal
        /// plugin; starting at the end of the collection.
        /// </summary>
        /// <param name="internalPlugin">
        /// The internal plugin to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index to begin the search.
        /// </param>
        /// <param name="count">
        /// The number of internal plugins to search.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to use in the search.
        /// </param>
        /// <returns>
        /// The zero-based index position of the internal
        /// plugin if it is found, or -1 if it is not. 
        /// </returns>
        public int LastIndexOf(ModAssemblyInternalPlugin? internalPlugin, int startIndex, int count, IEqualityComparer<ModAssemblyInternalPlugin>? equalityComparer)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, startIndex, count, equalityComparer);
        }

        /// <summary>
        /// Loads the internal plugin with the specified guid.
        /// </summary>
        /// <param name="guid">
        /// The guid of the internal plugin to load.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="guid"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// No such internal plugin with guid
        /// <paramref name="guid"/> exists.
        /// </exception>
        /// <exception cref="Exception">
        /// An exception occurs whilst loading the internal
        /// plugin.
        /// </exception>
        public void Load(string guid)
        {
            this[guid].Load();
        }

        /// <summary>
        /// Loads all the internal plugins.
        /// </summary>
        /// <exception cref="AggregateException">
        /// An aggregate of all exceptions thrown from
        /// loading the internal plugins.
        /// </exception>
        public void LoadAll()
        {
            List<Exception> exceptions = new();
            for (int index = 0; index < this.internalPlugins.Length; index++)
            {
                try
                {
                    this.internalPlugins[index].Load();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Loads all the internal plugins.
        /// </summary>
        /// <param name="loadFilter">
        /// The filter of whether to load the internal plugin.
        /// </param>
        /// <exception cref="AggregateException">
        /// An aggregate of all exceptions thrown from
        /// loading the internal plugins.
        /// </exception>
        public void LoadAll(Func<ModAssemblyInternalPlugin, bool>? loadFilter)
        {
            List<Exception> exceptions = new();
            for (int index = 0; index < this.internalPlugins.Length; index++)
            {
                ModAssemblyInternalPlugin internalPlugin = this.internalPlugins[index];

                if (loadFilter is not null && !loadFilter.Invoke(internalPlugin))
                    continue;

                try
                {
                    internalPlugin.Load();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        bool ICollection<ModAssemblyInternalPlugin>.Remove(ModAssemblyInternalPlugin item)
            => throw new NotSupportedException();
        void IList.Remove(object? value)
            => throw new NotSupportedException();

        void IList<ModAssemblyInternalPlugin>.RemoveAt(int index)
            => throw new NotSupportedException();
        void IList.RemoveAt(int index)
            => throw new NotSupportedException();

        /// <summary>
        /// Attempts to get the internal plugin with the
        /// specified guid.
        /// </summary>
        /// <param name="guid">
        /// The guid of the internal plugin to fetch.
        /// </param>
        /// <param name="internalPlugin">
        /// The found internal plugin.
        /// <para>
        /// This method will return whether or not this
        /// argument is not <see langword="null"/>.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if fetching the internal
        /// plugin was successful; otherwise, 
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGetInternalPlugin([NotNullWhen(true)] string? guid, [NotNullWhen(true)] out ModAssemblyInternalPlugin? internalPlugin)
        {
            if (guid is null)
            {
                internalPlugin = null;
                return false;
            }

            return this.guidToInternalPluginDictionary.TryGetValue(guid, out internalPlugin);
        }

        /// <summary>
        /// Unloads the internal plugin with the specified
        /// guid.
        /// </summary>
        /// <param name="guid">
        /// The guid of the internal plugin to unload.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="guid"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// No such internal plugin with guid
        /// <paramref name="guid"/> exists.
        /// </exception>
        /// <exception cref="Exception">
        /// An exception occurs whilst unloading the internal
        /// plugin.
        /// </exception>
        public void Unload(string guid)
        {
            this[guid].Unload();
        }

        /// <summary>
        /// Unloads all internal plugins.
        /// </summary>
        /// <exception cref="AggregateException">
        /// An aggregate of all exceptions thrown from
        /// unloading the internal plugins.
        /// </exception>
        public void UnloadAll()
        {
            List<Exception> exceptions = new();
            for (int index = this.internalPlugins.Length - 1; index >= 0; index--)
            {
                try
                {
                    this.internalPlugins[index].Unload();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// An enumerator to enumerate through a mod assembly
        /// internal plugin collection.
        /// </summary>
        public readonly struct Enumerator
        {
            private readonly ImmutableArray<ModAssemblyInternalPlugin>.Enumerator enumerator;

            internal Enumerator(InternalPluginCollection collection)
            {
                this.enumerator = collection.internalPlugins.GetEnumerator();
            }

            /// <summary>
            /// The current mod assembly internal plugin.
            /// </summary>
            public ModAssemblyInternalPlugin Current => this.enumerator.Current;

            /// <summary>
            /// Advances to the next mod assembly internal plugin
            /// in this collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if another mod assembly
            /// internal plugin exists in this collection;
            /// otherwise, <see langword="false"/>.
            /// </returns>
            public bool MoveNext()
                => this.enumerator.MoveNext();
        }
    }
}
