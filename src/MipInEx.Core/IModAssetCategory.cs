using System;
using System.Collections.Generic;

namespace MipInEx;

/// <summary>
/// An asset category of a mod.
/// </summary>
public interface IModAssetCategory
{
    /// <summary>
    /// The mod this asset category is for.
    /// </summary>
    ModInfo Mod { get; }

    /// <summary>
    /// The name of the category.
    /// </summary>
    /// <remarks>
    /// Is the singular name of this category.
    /// </remarks>
    string CategoryName { get; }

    /// <summary>
    /// The number of assets in this category.
    /// </summary>
    int AssetCount { get; }

    /// <summary>
    /// Gets the mod asset at the specified index.
    /// </summary>
    /// <param name="index">
    /// The index of the mod asset to get.
    /// </param>
    /// <returns>
    /// The mod asset at <paramref name="index"/>.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="index"/> is out of range.
    /// </exception>
    IModAsset this[int index] { get; }

    /// <summary>
    /// Gets all assets in this category.
    /// </summary>
    /// <returns>
    /// An enumerable of all assets in this category.
    /// </returns>
    IEnumerable<IModAsset> GetAssets();

    /// <inheritdoc/>
    IEnumerator<IModAsset> GetEnumerator();
}

/// <inheritdoc cref="IModAssetCategory"/>
/// <typeparam name="TAsset">
/// The type of assets in this asset category.
/// </typeparam>
public interface IModAssetCategory<TAsset> : IModAssetCategory
    where TAsset : IModAsset
{
    /// <inheritdoc cref="IModAssetCategory.this[int]"/>
    new TAsset this[int index] { get; }

    /// <inheritdoc cref="IModAssetCategory.GetAssets()"/>
    new IEnumerable<TAsset> GetAssets();

    /// <inheritdoc/>
    new IEnumerator<TAsset> GetEnumerator();
}
