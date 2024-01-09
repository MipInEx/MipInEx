using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace MipInEx.Bootstrap;

internal abstract class ModAssetBundleLoader : IModAssetLoader<AssetBundle>
{
    private readonly string name;
    private readonly string assetPath;

    protected ModAssetBundleLoader(string name, string assetPath)
    {
        this.name = name;
        this.assetPath = assetPath;
    }

    public string Name => this.name;
    public string AssetPath => this.assetPath;
    public string LongAssetPath => "Asset Bundles/" + this.assetPath;

    public abstract Stream OpenRead();
    public abstract void SaveToBinary(BinaryWriter binaryWriter);
    public abstract void SaveToZip(ZipArchive zipArchive);
    public abstract void SaveToModDirectory(string modDirectory);

    public abstract AssetBundle Load();
    object IModAssetLoader.Load()
        => this.Load();

    public abstract ModAsyncOperation<AssetBundle> LoadAsync();
    ModAsyncOperationWithResult IModAssetLoader.LoadAsync()
        => this.LoadAsync();

    public static FileLoader FromFile(string filePath, string assetPath)
    {
        return new FileLoader(filePath, assetPath);
    }

    public static BinaryLoader FromBinaryReader(BinaryReader reader)
    {
        // read the asset path of the asset bundle
        string assetPath = reader.ReadString();

        // read the length of the asset bundle asset
        int length = reader.ReadInt32();

        byte[] data = reader.ReadBytes(length);

        // now validate data as we have properly consumed the
        // data we needed from the reader.

        assetPath = Utility.ValidateAssetPath(assetPath);
        if (!assetPath.EndsWith(".bundle"))
            assetPath += ".bundle";

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
            "Asset Bundles",
            StringComparison.OrdinalIgnoreCase);

        return new BinaryLoader(name, fullAssetPath, data);
    }

    private sealed class LoadAsyncOperation : ModAsyncOperation<AssetBundle>
    {
        private readonly AssetBundleCreateRequest asyncOperation;
        private AssetBundle? result;
        private bool isDone;
        private float progress;

        public LoadAsyncOperation(AssetBundleCreateRequest asyncOperation)
        {
            this.asyncOperation = asyncOperation;

            this.result = null;

            this.isDone = false;
            this.progress = 0f;
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
        public sealed override AssetBundle? Result => this.result;

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

            this.result = this.asyncOperation.assetBundle;
            return true;
        }
    }

    public sealed class BinaryLoader : ModAssetBundleLoader
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

        public sealed override AssetBundle Load()
        {
            return AssetBundle.LoadFromMemory(this.data);
        }

        public sealed override ModAsyncOperation<AssetBundle> LoadAsync()
        {
            return new LoadAsyncOperation(AssetBundle.LoadFromMemoryAsync(this.data));
        }
    }

    public sealed class FileLoader : ModAssetBundleLoader
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

        public sealed override AssetBundle Load()
        {
            return AssetBundle.LoadFromFile(this.filePath);
        }

        public sealed override ModAsyncOperation<AssetBundle> LoadAsync()
        {
            return new LoadAsyncOperation(AssetBundle.LoadFromFileAsync(this.filePath));
        }
    }
}
