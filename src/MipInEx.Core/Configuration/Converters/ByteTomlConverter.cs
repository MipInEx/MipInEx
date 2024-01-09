using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="byte"/> Converter.
/// </summary>
public sealed class ByteTomlConverter : SpecializedTomlTypeConverter<byte>
{
    /// <inheritdoc/>
    public sealed override string Serialize(byte value, Type targetType)
    {
        return value.ToString();
    }

    /// <inheritdoc/>
    public sealed override byte Deserialize(string value, Type targetType)
    {
        return byte.Parse(value);
    }
}
