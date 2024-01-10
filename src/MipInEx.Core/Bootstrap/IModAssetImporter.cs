using System;
using System.IO;
using System.IO.Compression;
using System.Security;

namespace MipInEx.Bootstrap;

/// <summary>
/// Represents an importer for a mod asset.
/// </summary>
internal interface IModAssetImporter
{
    /// <summary>
    /// The name of the asset being imported. This excludes
    /// the file extension.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The full/long asset path of the asset being imported.
    /// This will include the file extension.
    /// </summary>
    string FullAssetPath { get; }

    /// <summary>
    /// Creates a new memory stream to read the contents of the
    /// asset.
    /// </summary>
    /// <returns>
    /// A memory stream that can be read to retrieve the
    /// contents of the asset.
    /// </returns>
    MemoryStream OpenRead();

    /// <summary>
    /// Exports this mod asset to the specified binary writer.
    /// </summary>
    /// <param name="binaryWriter">
    /// The binary writer to write the bytes of this asset to.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// The stream of the binary writer was closed.
    /// </exception>
    /// <exception cref="IOException">
    /// An I/O error occured whilst writing to the binary
    /// writer.
    /// </exception>
    void ExportToBinary(BinaryWriter binaryWriter);

    /// <summary>
    /// Exports this mod asset to the specified zip archive.
    /// </summary>
    /// <param name="zipArchive">
    /// The zip archive to write this asset to.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// The zip archive does not support writing.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The zip archive has been disposed.
    /// </exception>
    void ExportToZip(ZipArchive zipArchive);

    /// <summary>
    /// Exports this mod asset to the specified mod directory.
    /// </summary>
    /// <param name="modDirectory">
    /// The mod directory to export the asset to.
    /// </param>
    /// <exception cref="PathTooLongException">
    /// The full path of the file to write to exceeds the
    /// system-defined maximum length.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// The file to write to is invalid, or the path to the
    /// directory to create to write the file to is invalid
    /// (for example, it is on an unmapped drive).
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The full path of the file to write to is in an invalid
    /// format, or the path of the directory to write the file
    /// to or the full path of the file to write to contains a
    /// colon character (:) that is not part of a drive label
    /// ("C:\").
    /// </exception>
    /// <exception cref="IOException">
    /// An I/O error occurred while writing the file, or
    /// creating it's parent's directory.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// This operation is not supported on the current
    /// platform, the full path of the file to write to is to a
    /// file that is read-only or hidden, or the caller
    /// does not have the required permission.
    /// </exception>
    /// <exception cref="SecurityException">
    /// The caller does not have the required permission.
    /// </exception>
    void ExportToModDirectory(string modDirectory);

    /// <summary>
    /// Imports this asset synchronously.
    /// </summary>
    /// <returns>
    /// The imported asset.
    /// </returns>
    object Import();

    /// <summary>
    /// Imports this asset asynchronously;
    /// </summary>
    /// <returns>
    /// An async operation that when completed will contain the
    /// imported asset.
    /// </returns>
    ModAsyncOperationWithResult ImportAsync();
}

/// <inheritdoc cref="IModAssetImporter"/>
/// <typeparam name="TResult">
/// The type of result achieved from importing the asset.
/// </typeparam>
internal interface IModAssetImporter<TResult> : IModAssetImporter
    where TResult : notnull
{
    /// <inheritdoc cref="IModAssetImporter.Import"/>
    new TResult Import();
    /// <inheritdoc cref="IModAssetImporter.ImportAsync"/>
    new ModAsyncOperation<TResult> ImportAsync();
}
