#if UNITY_ENGINE
using UnityEngine;
#endif

using System;
using System.Collections.Generic;
using System.Text;
using MipInEx.Bootstrap;

namespace MipInEx;


/// <summary>
/// A mod asset bundle.
/// </summary>
public sealed class ModAssetBundle : IModAsset
{
    private readonly ModAssetBundleInfo info;
    private readonly Mod mod;
    private readonly ModAssetBundleImporter importer;
    private readonly ModAssetBundleManifest manifest;

#if UNITY_ENGINE
    private AssetBundle? instance;
#endif

    private LoadAsyncOperation? loadOperation;
    private UnloadAsyncOperation? unloadOperation;

    internal ModAssetBundle(
        Mod mod,
        ModAssetBundleManifest manifest,
        ModAssetBundleImporter importer)
    {
        this.mod = mod;
        this.importer = importer;
        this.manifest = manifest;
        this.instance = null;
        this.loadOperation = null;
        this.unloadOperation = null;

        this.info = new ModAssetBundleInfo(
            importer.Name,
            Utility.ShortenAssetPath(
                importer.FullAssetPath,
                "Asset Bundles",
                StringComparison.OrdinalIgnoreCase),
            importer.FullAssetPath,
            this);
    }

    internal ModAssetBundleImporter Importer => this.importer;

    /// <summary>
    /// The mod this asset bundle belongs to.
    /// </summary>
    public Mod Mod => this.mod;

    /// <summary>
    /// The mod manager that manages the mod this asset bundle
    /// belongs to.
    /// </summary>
    public ModManagerBase ModManager => this.mod.ModManager;

    /// <summary>
    /// The manifest of this asset bundle.
    /// </summary>
    public ModAssetBundleManifest Manifest => this.manifest;
    IModAssetManifest IModAsset.Manifest => this.manifest;

    /// <summary>
    /// The info about this asset bundle.
    /// </summary>
    public ModAssetBundleInfo Info => this.info;
    IModAssetInfo IModAsset.Info => this.info;

    /// <summary>
    /// The name of the asset bundle.
    /// </summary>
    public string Name => this.info.Name;

    /// <summary>
    /// The path of the asset bundle asset. 
    /// </summary>
    public string AssetPath => this.info.AssetPath;

    /// <summary>
    /// The full asset path of the asset bundle asset.
    /// </summary>
    public string FullAssetPath => this.info.FullAssetPath;

    string IModAsset.AssetPath => this.info.AssetPath;
    string IModAsset.FullAssetPath => this.info.FullAssetPath;

    /// <summary>
    /// The state of this asset bundle.
    /// </summary>
    public ModAssetState State => this.info.State;

    /// <summary>
    /// Whether or not this asset bundle is loaded.
    /// </summary>
    public bool IsLoaded => this.instance != null && this.info.IsLoaded;

    /// <summary>
    /// The type of the mod asset.
    /// </summary>
    /// <remarks>
    /// Will always return
    /// <see cref="ModAssetType.AssetBundle"/>.
    /// </remarks>
    public ModAssetType Type => ModAssetType.AssetBundle;

    /// <summary>
    /// Loads the asset bundle synchronously.
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
    /// Thrown if the asset bundle is unloading asynchronously
    /// or an asynchronous load request is active.
    /// </exception>
    public void Load()
    {
        if (this.IsLoaded)
            return;
        else if (this.unloadOperation != null)
            throw new InvalidOperationException("Cannot load whilst the asset bundle is being unloaded!");
        else if (this.loadOperation != null)
            throw new InvalidOperationException("An existing async load request is already active!");

        this.instance = this.importer.Import();
        this.info.SetState(ModAssetState.Loaded);
    }

    /// <summary>
    /// Loads the asset bundle asynchronously.
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
    /// <see cref="ModAsyncOperation.Completed"/> if the asset
    /// bundle is already loaded, or a faulted
    /// <see cref="ModAsyncOperation"/> with an exception
    /// of type <see cref="InvalidOperationException"/> if the
    /// asset bundle is being unloaded asynchronously.
    /// </returns>
    public ModAsyncOperation LoadAsync()
    {
        if (this.IsLoaded)
            return ModAsyncOperation.Completed;
        else if (this.unloadOperation != null)
            return ModAsyncOperation.FromException(new InvalidOperationException("Cannot load whilst the asset bundle is being unloaded!"));

        this.loadOperation ??= new LoadAsyncOperation(this);
        return this.loadOperation;
    }

    /// <summary>
    /// Unloads the asset bundle synchronously.
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
    /// Thrown if the asset bundle is loading asynchronously or
    /// an asynchronous unload request is active.
    /// </exception>
    public void Unload()
    {
        if (!this.IsLoaded)
            return;
        else if (this.loadOperation != null)
            throw new InvalidOperationException("Cannot unload whilst the asset bundle is being loaded!");
        else if (this.unloadOperation != null)
            throw new InvalidOperationException("An existing async unload request is already active!");

        this.instance!.Unload(true);
        this.info.SetState(ModAssetState.Unloaded);
    }


    /// <summary>
    /// Unloads the asset bundle asynchronously.
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
    /// <see cref="ModAsyncOperation.Completed"/> if the asset
    /// bundle is already unloaded, or a faulted
    /// <see cref="ModAsyncOperation"/> with an exception
    /// of type <see cref="InvalidOperationException"/> if the
    /// asset bundle is being loaded asynchronously.
    /// </returns>
    public ModAsyncOperation UnloadAsync()
    {
        if (this.loadOperation != null)
            return ModAsyncOperation.FromException(new InvalidOperationException("Cannot unload whilst the asset bundle is being loaded!"));
        else if (!this.IsLoaded)
            return ModAsyncOperation.Completed;

        this.unloadOperation ??= new UnloadAsyncOperation(this);
        return this.unloadOperation;
    }

    /// <summary>
    /// Gets the descriptor string for this asset bundle.
    /// </summary>
    /// <returns>
    /// The descriptor string of this asset bundle.
    /// </returns>
    public string GetDescriptorString()
    {
        return $"Asset Bundle '{this.Name}'";
    }

    /// <inheritdoc/>
    public sealed override string ToString()
    {
        return this.GetDescriptorString();
    }

#if UNITY_ENGINE
    /// <inheritdoc cref="AssetBundle.Contains(string)"/>
    public bool Contains(string name)
    {
        if (this.instance == null) return false;
        else return this.instance.Contains(name);
    }

    /// <inheritdoc cref="AssetBundle.GetAllAssetNames()"/>
    public string[] GetAllAssetNames()
    {
        if (this.instance == null) return Array.Empty<string>();
        else return this.instance.GetAllAssetNames();
    }

    /// <inheritdoc cref="AssetBundle.LoadAsset(string)"/>
    public UnityEngine.Object? LoadAsset(string name)
    {
        if (this.instance == null) return null;
        else return this.instance.LoadAsset(name);
    }

    /// <inheritdoc cref="AssetBundle.LoadAsset(string, Type)"/>
    public UnityEngine.Object? LoadAsset(string name, Type type)
    {
        if (this.instance == null) return null;
        else return this.instance.LoadAsset(name, type);
    }

    /// <inheritdoc cref="AssetBundle.LoadAsset{T}(string)"/>
    public T? LoadAsset<T>(string name)
        where T : UnityEngine.Object
    {
        if (this.instance == null) return null;
        else return this.instance.LoadAsset<T>(name);
    }

    /// <inheritdoc cref="AssetBundle.LoadAllAssets()"/>
    public UnityEngine.Object[] LoadAllAssets()
    {
        if (this.instance == null) return Array.Empty<UnityEngine.Object>();
        else return this.instance.LoadAllAssets();
    }

    /// <inheritdoc cref="AssetBundle.LoadAllAssets(Type)"/>
    public UnityEngine.Object[] LoadAllAssets(Type type)
    {
        if (this.instance == null) return Array.Empty<UnityEngine.Object>();
        else return this.instance.LoadAllAssets(type);
    }

    /// <inheritdoc cref="AssetBundle.LoadAllAssets{T}()"/>
    public T[] LoadAllAssets<T>()
        where T : UnityEngine.Object
    {
        if (this.instance == null) return Array.Empty<T>();
        else return this.instance.LoadAllAssets<T>();
    }

    /// <inheritdoc cref="AssetBundle.LoadAssetWithSubAssets(string)"/>
    public UnityEngine.Object[] LoadAssetWithSubAssets(string name)
    {
        if (this.instance == null) return Array.Empty<UnityEngine.Object>();
        else return this.instance.LoadAssetWithSubAssets(name);
    }

    /// <inheritdoc cref="AssetBundle.LoadAssetWithSubAssets(string, Type)"/>
    public UnityEngine.Object[] LoadAssetWithSubAssets(string name, Type type)
    {
        if (this.instance == null) return Array.Empty<UnityEngine.Object>();
        else return this.instance.LoadAssetWithSubAssets(name, type);
    }

    /// <inheritdoc cref="AssetBundle.LoadAssetWithSubAssets{T}(string)"/>
    public T[] LoadAssetWithSubAssets<T>(string name)
        where T : UnityEngine.Object
    {
        if (this.instance == null) return Array.Empty<T>();
        else return this.instance.LoadAssetWithSubAssets<T>(name);
    }
#endif

    private sealed class LoadAsyncOperation : ModAsyncOperation
    {
        private readonly ModAssetBundle assetBundleInfo;
        private readonly ModAsyncOperation<AssetBundle> asyncOperation;
        private bool isDone;

        public LoadAsyncOperation(ModAssetBundle assetBundleInfo)
        {
            this.assetBundleInfo = assetBundleInfo;
            this.assetBundleInfo.info.SetState(ModAssetState.Loading);

            this.asyncOperation = assetBundleInfo.importer.ImportAsync();
            this.isDone = false;
        }

        public sealed override ModAsyncOperationStatus Status => this.asyncOperation.Status;
        public sealed override bool IsRunning => this.asyncOperation.IsRunning;
        public sealed override bool IsCompleted => this.asyncOperation.IsCompleted;
        public sealed override bool IsCompletedSuccessfully => this.asyncOperation.IsCompletedSuccessfully;
        public sealed override bool IsFaulted => false;
        public sealed override Exception? Exception => null;

        public sealed override double GetProgress()
            => this.asyncOperation.GetProgress();

        public sealed override bool Process()
        {
            if (this.isDone) return true;

            this.isDone = this.asyncOperation.Process();

            if (!this.isDone)
            {
                return false;
            }

            this.assetBundleInfo.instance = this.asyncOperation.Result;
            this.assetBundleInfo.loadOperation = null;
            this.assetBundleInfo.info.SetState(ModAssetState.Loading);
            return true;
        }
    }

    private sealed class UnloadAsyncOperation : ModAsyncOperation
    {
        private readonly ModAssetBundle assetBundleInfo;
        private readonly AssetBundleUnloadOperation asyncOperation;
        private bool isDone;
        private float progress;

        public UnloadAsyncOperation(ModAssetBundle assetBundleInfo)
        {
            this.assetBundleInfo = assetBundleInfo;
            this.asyncOperation = assetBundleInfo.instance!.UnloadAsync(true);
            this.isDone = false;
            this.progress = 0f;

            this.assetBundleInfo.info.SetState(ModAssetState.Unloading);

            assetBundleInfo.instance = null;
        }

        public sealed override ModAsyncOperationStatus Status
        {
            get
            {
                if (this.isDone) return ModAsyncOperationStatus.SuccessComplete;
                else return ModAsyncOperationStatus.Running;
            }
        }
        public sealed override bool IsRunning => !this.isDone;
        public sealed override bool IsCompleted => this.isDone;
        public sealed override bool IsCompletedSuccessfully => this.isDone;
        public sealed override bool IsFaulted => false;
        public sealed override Exception? Exception => null;

        public sealed override double GetProgress()
        {
            return this.progress;
        }

        public sealed override bool Process()
        {
            if (this.isDone) return true;

            this.isDone = this.asyncOperation.isDone;
            this.progress = this.asyncOperation.progress;

            if (!this.isDone)
            {
                return false;
            }

            this.assetBundleInfo.unloadOperation = null;
            this.assetBundleInfo.info.SetState(ModAssetState.Unloaded);
            return true;
        }
    }
}
