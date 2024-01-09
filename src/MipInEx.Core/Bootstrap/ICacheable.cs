using System.IO;

namespace MipInEx.Bootstrap;

internal interface ICacheable
{
    /// <summary>
    /// Serializes the object into a binary format.
    /// </summary>
    /// <param name="binaryWriter">
    /// The binary writer to write to.
    /// </param>
    void Save(BinaryWriter binaryWriter);

    /// <summary>
    /// Loads the object from binary format.
    /// </summary>
    /// <param name="binaryReader">
    /// The binary reader to read from.
    /// </param>
    void Load(BinaryReader binaryReader);
}
