﻿using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security;

namespace MipInEx.Bootstrap;

internal abstract class ModAssemblyImporter : ModAssetImporterBase<Assembly>
{
    private Assembly? assembly;

    /// <summary>
    /// Initializes this mod assembly importer with the
    /// specified name and asset path.
    /// </summary>
    /// <param name="name">
    /// The name of the assembly (excluding file extension)
    /// </param>
    /// <param name="fullAssetPath">
    /// The full asset path of the assembly.
    /// </param>
    protected ModAssemblyImporter(string name, string fullAssetPath)
        : base(name, fullAssetPath)
    {
        this.assembly = null;
    }

    public static ModAssemblyImporter FromFile(string filePath, string assetPath)
    {
        return new FileImporter(filePath, assetPath);
    }

    public static ModAssemblyImporter FromBinaryReader(BinaryReader reader)
    {
        // read the asset path of the assembly
        string assetPath = reader.ReadString();

        // read the length of the assembly asset
        int length = reader.ReadInt32();

        byte[] data = reader.ReadBytes(length);

        // now validate data as we have properly consumed the
        // data we needed from the reader.

        assetPath = Utility.ValidateAssetPath(assetPath);
        if (!assetPath.EndsWith(".dll"))
            assetPath += ".dll";

        return new BinaryImporter(data, assetPath);
    }

    public static ModAssemblyImporter FromZipArchive(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using MemoryStream copyStream = new();
        stream.CopyTo(copyStream);
        byte[] data = copyStream.ToArray();

        string name = Path.GetFileNameWithoutExtension(entry.Name);
        string fullAssetPath = Utility.ShortenAssetPath(
            entry.FullName,
            "Assemblies",
            StringComparison.OrdinalIgnoreCase);

        return new BinaryImporter(data, name, fullAssetPath);
    }
    
    /// <summary>
    /// The file importer.
    /// </summary>
    private sealed class FileImporter : ModAssemblyImporter
    {
        private readonly string filePath;

        /// <summary>
        /// Initializes this file importer with the specified
        /// file path and asset path.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file.
        /// </param>
        /// <param name="fullAssetPath">
        /// The full asset path of the asset.
        /// </param>
        public FileImporter(string filePath, string fullAssetPath)
            : base(Path.GetFileNameWithoutExtension(filePath), fullAssetPath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// The full path to the file.
        /// </summary>
        public string FilePath => this.filePath;

        /// <inheritdoc/>
        /// <remarks>
        /// This method first reads all the bytes of the file
        /// at the <see cref="FilePath">file path</see>, then
        /// creates a new memory stream with the bytes read.
        /// </remarks>
        /// <exception cref="PathTooLongException">
        /// This importer's file path exceeds the
        /// system-defined maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// This importer's file path is invalid (for example,
        /// it is on an unmapped drive).
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred while opening the file.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// This operation is not supported on the current
        /// platform, this importer's file path is a directory,
        /// or the caller does not have the required permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The file at this importer's file path was not
        /// found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// This importer's file path is in an invalid format.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The caller does not have the required permission.
        /// </exception>
        public sealed override MemoryStream OpenRead()
        {
            return new MemoryStream(File.ReadAllBytes(this.FilePath));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This method first writes the full asset path to the
        /// writer, then writes the size of the bytes of the
        /// file at the <see cref="FilePath">file path</see> to
        /// the writer, then writes the bytes themselves.
        /// <para>
        /// Exceptions thrown from reading all bytes of the
        /// file will get thrown after writing zero (the size)
        /// to the writer.
        /// </para>
        /// </remarks>
        /// <exception cref="PathTooLongException">
        /// This importer's file path exceeds the
        /// system-defined maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// This importer's file path is invalid (for example,
        /// it is on an unmapped drive).
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred while opening the file, or an
        /// I/O error occured whilst writing to the binary
        /// writer.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// This operation is not supported on the current
        /// platform, this importer's file path is a directory,
        /// or the caller does not have the required permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The file at this importer's file path was not
        /// found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// This importer's file path is in an invalid format.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The caller does not have the required permission.
        /// </exception>
        public sealed override void ExportToBinary(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(this.FullAssetPath);

            byte[] data;
            try
            {
                data = File.ReadAllBytes(this.FilePath);
            }
            catch
            {
                binaryWriter.Write(0);
                throw;
            }

            binaryWriter.Write(data.Length);
            binaryWriter.Write(data);
        }

        /// <inheritdoc/>
        /// <exception cref="PathTooLongException">
        /// This importer's file path exceeds the
        /// system-defined maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// This importer's file path is invalid (for example,
        /// it is on an unmapped drive).
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred while opening the file.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// This operation is not supported on the current
        /// platform, this importer's file path is a directory,
        /// or the caller does not have the required permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The file at this importer's file path was not
        /// found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// This importer's file path is in an invalid format,
        /// or the zip archive does not support writing.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The caller does not have the required permission.
        /// </exception>
        public sealed override void ExportToZip(ZipArchive zipArchive)
        {
            ZipArchiveEntry entry = zipArchive.CreateEntry(this.FullAssetPath);
            using Stream writeStream = entry.Open();
            using BinaryWriter binaryWriter = new BinaryWriter(writeStream);
            binaryWriter.Write(File.ReadAllBytes(this.FilePath));
        }

        /// <inheritdoc/>
        /// <exception cref="PathTooLongException">
        /// This importer's file path exceeds the
        /// system-defined maximum length, or the full path of
        /// the file to write to exceeds the system-defined
        /// maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// This importer's file path is invalid, the full path
        /// of the file to write to is invalid, or the path to
        /// the directory to create to write the file to is
        /// invalid (for example,
        /// it is on an unmapped drive).
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred while opening the file,
        /// writing the file, or creating it's parent's
        /// directory.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// This operation is not supported on the current
        /// platform, this importer's file path is a directory,
        /// the full path of the file to write to is to a file
        /// that is read-only or hidden, or the caller does not
        /// have the required permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The file at this importer's file path was not
        /// found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// This importer's file path is in an invalid format,
        /// the full path of the file to write to is in an
        /// invalid format, or the path of the directory to
        /// write the file to or the full path of the file to
        /// write to contains a colon character (:) that is not
        /// part of a drive label ("C:\").
        /// </exception>
        /// <exception cref="SecurityException">
        /// The caller does not have the required permission.
        /// </exception>
        public sealed override void ExportToModDirectory(string modDirectory)
        {
            string fullPath = Path.Combine(modDirectory, this.FullAssetPath);
            string parentDirectory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllBytes(fullPath, File.ReadAllBytes(this.FilePath));
        }

        /// <inheritdoc/>
        public sealed override Assembly Import()
        {
            if (this.assembly == null)
            {
                // sad boy hours: no span overload :((((
                this.assembly = Assembly.LoadFrom(this.FilePath);
            }

            return this.assembly;
        }

        /// <inheritdoc/>
        public sealed override ModAsyncOperation<Assembly> ImportAsync()
        {
            return ModAsyncOperation.FromOperation(this.Import);
        }
    }

    /// <summary>
    /// The binary importer.
    /// </summary>
    private sealed class BinaryImporter : ModAssemblyImporter
    {
        private readonly byte[] data;

        /// <summary>
        /// Initializes this binary importer with the specified
        /// data and asset path.
        /// <para>
        /// The name of the asset is found by calling
        /// <see cref="Path.GetFileNameWithoutExtension(string)"/>
        /// on <paramref name="fullAssetPath"/>.
        /// </para>
        /// </summary>
        /// <param name="fullAssetPath">
        /// The full asset path of the asset.
        /// </param>
        /// <param name="data">
        /// The raw byte data for this importer.
        /// </param>
        public BinaryImporter(byte[] data, string fullAssetPath)
            : base(Path.GetFileNameWithoutExtension(fullAssetPath), fullAssetPath)
        {
            this.data = data;
        }

        /// <summary>
        /// Initializes this binary importer with the specified
        /// data, name, and asset path.
        /// </summary>
        /// <param name="fullAssetPath">
        /// The full asset path of the asset.
        /// </param>
        /// <param name="name">
        /// The name of the asset (excluding file extension)
        /// </param>
        /// <param name="data">
        /// The raw byte data for this importer.
        /// </param>
        public BinaryImporter(byte[] data, string name, string fullAssetPath)
            : base(name, fullAssetPath)
        {
            this.data = data;
        }

        /// <summary>
        /// The byte data in this binary importer.
        /// </summary>
        public byte[] Data => this.data;

        /// <inheritdoc/>
        /// <remarks>
        /// This method just creates a memory stream wrapping
        /// this importer's <see cref="Data">data</see>.
        /// </remarks>
        public sealed override MemoryStream OpenRead()
        {
            return new MemoryStream(this.Data);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This method first writes the full asset path to the
        /// writer, then writes the size of the bytes of this
        /// importer's <see cref="Data">data</see> to
        /// the writer, then writes this importer's
        /// <see cref="Data">data</see>.
        /// </remarks>
        public sealed override void ExportToBinary(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(this.FullAssetPath);
            binaryWriter.Write(this.Data.Length);
            binaryWriter.Write(this.Data);
        }

        /// <inheritdoc/>
        public sealed override void ExportToZip(ZipArchive zipArchive)
        {
            ZipArchiveEntry entry = zipArchive.CreateEntry(this.FullAssetPath);
            using Stream writeStream = entry.Open();
            using BinaryWriter binaryWriter = new BinaryWriter(writeStream);
            binaryWriter.Write(this.Data);
        }

        /// <inheritdoc/>
        public sealed override void ExportToModDirectory(string modDirectory)
        {
            string fullPath = Path.Combine(modDirectory, this.FullAssetPath);
            string parentDirectory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllBytes(fullPath, this.Data);
        }

        /// <inheritdoc/>
        public sealed override Assembly Import()
        {
            if (this.assembly == null)
            {
                // sad boy hours: no span overload :((((
                this.assembly = Assembly.Load(this.data);
            }

            return this.assembly;
        }

        /// <inheritdoc/>
        public sealed override ModAsyncOperation<Assembly> ImportAsync()
        {
            return ModAsyncOperation.FromOperation(this.Import);
        }
    }
}
