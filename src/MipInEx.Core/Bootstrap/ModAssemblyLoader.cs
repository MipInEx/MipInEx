using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace MipInEx.Bootstrap;

internal abstract class ModAssemblyLoader : IModAssetLoader<Assembly>
{
    private readonly string name;
    private readonly string assetPath;
    private Assembly? assembly;

    protected ModAssemblyLoader(string name, string assetPath)
    {
        this.name = name;
        this.assetPath = assetPath;
        this.assembly = null;
    }

    public string Name => this.name;
    public string AssetPath => this.assetPath;
    public string LongAssetPath => "Assemblies/" + this.assetPath;

    public abstract Stream OpenRead();
    public abstract void SaveToBinary(BinaryWriter binaryWriter);
    public abstract void SaveToZip(ZipArchive zipArchive);
    public abstract void SaveToModDirectory(string modDirectory);

    public abstract Assembly Load();
    object IModAssetLoader.Load()
        => this.Load();

    ModAsyncOperation<Assembly> IModAssetLoader<Assembly>.LoadAsync()
        => ModAsyncOperation.FromOperation(this.Load);
    ModAsyncOperationWithResult IModAssetLoader.LoadAsync()
        => ModAsyncOperation.FromOperation(this.Load);

    public static FileLoader FromFile(string filePath, string assetPath)
    {
        return new FileLoader(filePath, assetPath);
    }

    public static BinaryLoader FromBinaryReader(BinaryReader reader)
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

        return new BinaryLoader(assetPath, data);
    }

    public static BinaryLoader FromZipArchive(ZipArchiveEntry entry)
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

        return new BinaryLoader(name, fullAssetPath, data);
    }

    public sealed class BinaryLoader : ModAssemblyLoader
    {
        private readonly byte[] data;

        public BinaryLoader(string assetPath, byte[] data)
            : base(Path.GetFileNameWithoutExtension(assetPath), assetPath)
        {
            this.data = data;
        }

        public BinaryLoader(string name, string assetPath, byte[] data)
            : base(name, assetPath)
        {
            this.data = data;
        }

        public sealed override Stream OpenRead()
        {
            return new MemoryStream(this.data);
        }

        public sealed override void SaveToBinary(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(this.assetPath);
            binaryWriter.Write(this.data.Length);
            binaryWriter.Write(this.data);
        }

        public sealed override void SaveToZip(ZipArchive zipArchive)
        {
            ZipArchiveEntry entry = zipArchive.CreateEntry(this.assetPath);
            using Stream writeStream = entry.Open();
            using BinaryWriter binaryWriter = new BinaryWriter(writeStream);
            binaryWriter.Write(this.data);
        }

        public sealed override void SaveToModDirectory(string modDirectory)
        {
            string fullPath = Path.Combine(modDirectory, this.assetPath);
            string parentDirectory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllBytes(fullPath, this.data);
        }

        public sealed override Assembly Load()
        {
            if (this.assembly == null)
            {
                // sad boy hours: no span overload :((((
                this.assembly = Assembly.Load(this.data);
            }

            return this.assembly;
        }
    }

    public sealed class FileLoader : ModAssemblyLoader
    {
        private readonly string filePath;

        public FileLoader(string filePath, string assetPath)
            : base(Path.GetFileNameWithoutExtension(filePath), assetPath)
        {
            this.filePath = filePath;
        }

        public sealed override Stream OpenRead()
        {
            return new MemoryStream(File.ReadAllBytes(this.filePath));
        }

        public sealed override void SaveToBinary(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(this.assetPath);

            byte[] data = File.ReadAllBytes(this.filePath);
            binaryWriter.Write(data.Length);
            binaryWriter.Write(data);
        }

        public sealed override void SaveToZip(ZipArchive zipArchive)
        {
            ZipArchiveEntry entry = zipArchive.CreateEntry(this.assetPath);
            using Stream writeStream = entry.Open();
            using BinaryWriter binaryWriter = new BinaryWriter(writeStream);
            binaryWriter.Write(File.ReadAllBytes(this.filePath));
        }

        public sealed override void SaveToModDirectory(string modDirectory)
        {
            string fullPath = Path.Combine(modDirectory, this.assetPath);
            string parentDirectory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllBytes(fullPath, File.ReadAllBytes(this.filePath));
        }

        public sealed override Assembly Load()
        {
            if (this.assembly == null)
            {
                this.assembly = Assembly.Load(this.filePath);
            }

            return this.assembly;
        }
    }
}
