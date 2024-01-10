using System;

namespace MipInEx;

/// <summary>
/// An asset of a mod.
/// </summary>
public interface IModAsset
{
    /// <summary>
    /// The name of this asset.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The manifest of this asset.
    /// </summary>
    IModAssetManifest Manifest { get; }

    /// <summary>
    /// The mod this mod asset is from.
    /// </summary>
    Mod Mod { get; }

    /// <summary>
    /// The mod manager that manages the mod this asset belongs
    /// to.
    /// </summary>
    ModManagerBase ModManager { get; }

    /// <summary>
    /// The asset path of this mod asset.
    /// </summary>
    string AssetPath { get; }

    /// <summary>
    /// The full asset path of this mod asset.
    /// </summary>
    string FullAssetPath { get; }

    /// <summary>
    /// The state of the mod asset.
    /// </summary>
    ModAssetState State { get; }

    /// <summary>
    /// Whether or not the mod asset is loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// The info about the mod asset.
    /// </summary>
    IModAssetInfo Info { get; }

    /// <summary>
    /// The type of the mod asset.
    /// </summary>
    ModAssetType Type { get; }

    /// <summary>
    /// Gets the descriptor string for this mod asset.
    /// </summary>
    /// <returns>
    /// The descriptor string of this mod asset.
    /// </returns>
    string GetDescriptorString();

    /// <summary>
    /// Loads the mod asset synchronously.
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
    /// Thrown if the mod asset is unloading asynchronously or
    /// an asynchronous load request is active.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Loading this mod asset isn't supported.
    /// </exception>
    void Load();

    /// <summary>
    /// Loads the mod asset asynchronously.
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
    /// <see cref="ModAsyncOperation.Completed"/> if the mod
    /// asset is already loaded, a faulted
    /// <see cref="ModAsyncOperation"/> with an exception
    /// of type <see cref="InvalidOperationException"/> if the
    /// mod asset is being unloaded asynchronously, or a
    /// faulted <see cref="ModAsyncOperation"/> with an
    /// exception of type <see cref="NotSupportedException"/>
    /// if loading this mod asset isn't supported.
    /// </returns>
    ModAsyncOperation LoadAsync();

    /// <summary>
    /// Unloads the mod asset synchronously.
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
    /// Thrown if the mod asset is loading asynchronously or
    /// an asynchronous unload request is active.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Unloading this mod asset isn't supported.
    /// </exception>
    void Unload();

    /// <summary>
    /// Unloads the mod asset asynchronously.
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
    /// <see cref="ModAsyncOperation.Completed"/> if the mod
    /// asset is already unloaded, a faulted
    /// <see cref="ModAsyncOperation"/> with an exception
    /// of type <see cref="InvalidOperationException"/> if the
    /// mod asset is being loaded asynchronously, or a
    /// faulted <see cref="ModAsyncOperation"/> with an
    /// exception of type <see cref="NotSupportedException"/>
    /// if unloading this mod asset isn't supported.
    /// </returns>
    ModAsyncOperation UnloadAsync();
}
