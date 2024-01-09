using System.IO;
using System.IO.Compression;

namespace MipInEx.Bootstrap;

internal interface IModAssetLoader
{
    string Name { get; }
    string AssetPath { get; }

    Stream OpenRead();
    void SaveToBinary(BinaryWriter binaryWriter);
    void SaveToZip(ZipArchive zipArchive);
    void SaveToModDirectory(string modDirectory);

    object Load();
    ModAsyncOperationWithResult LoadAsync();
}

internal interface IModAssetLoader<TResult> : IModAssetLoader
    where TResult : notnull
{
    new TResult Load();
    new ModAsyncOperation<TResult> LoadAsync();
}
