using System;
using System.IO;
using System.IO.Compression;
using System.Security;

namespace MipInEx.Bootstrap;

/// <summary>
/// The base mod asset importer.
/// </summary>
/// <typeparam name="TResult">
/// The result from importing the mod asset.
/// </typeparam>
internal abstract class ModAssetImporterBase<TResult> : IModAssetImporter<TResult>
    where TResult : notnull
{
    private readonly string name;
    private readonly string fullAssetPath;

    /// <summary>
    /// Initializes this mod asset importer with the specified
    /// name and asset path.
    /// </summary>
    /// <param name="name">
    /// The name of the asset (excluding file extension)
    /// </param>
    /// <param name="fullAssetPath">
    /// The full asset path of the asset.
    /// </param>
    protected ModAssetImporterBase(string name, string fullAssetPath)
    {
        this.name = name;
        this.fullAssetPath = fullAssetPath;
    }

    /// <inheritdoc cref="IModAssetImporter.Name"/>
    public string Name => this.name;
    /// <inheritdoc cref="IModAssetImporter.FullAssetPath"/>
    public string FullAssetPath => this.fullAssetPath;

    /// <inheritdoc cref="IModAssetImporter.OpenRead()"/>
    public abstract MemoryStream OpenRead();

    /// <inheritdoc cref="IModAssetImporter.ExportToBinary(BinaryWriter)"/>
    public abstract void ExportToBinary(BinaryWriter binaryWriter);
    /// <inheritdoc cref="IModAssetImporter.ExportToZip(ZipArchive)"/>
    public abstract void ExportToZip(ZipArchive zipArchive);
    /// <inheritdoc cref="IModAssetImporter.ExportToModDirectory(string)"/>
    public abstract void ExportToModDirectory(string modDirectory);

    /// <inheritdoc cref="IModAssetImporter{TResult}.Import()"/>
    public abstract TResult Import();
    object IModAssetImporter.Import() => this.Import();

    /// <inheritdoc cref="IModAssetImporter{TResult}.ImportAsync()"/>
    public abstract ModAsyncOperation<TResult> ImportAsync();
    ModAsyncOperationWithResult IModAssetImporter.ImportAsync() => this.ImportAsync();
}
