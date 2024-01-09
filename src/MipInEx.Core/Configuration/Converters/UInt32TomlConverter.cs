using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// An <see cref="uint"/> Converter.
/// </summary>
public sealed class UInt32TomlConverter : SpecializedTomlTypeConverter<uint>
{
    /// <inheritdoc/>
    public sealed override string Serialize(uint value, Type targetType)
    {
        return value.ToString();
    }

    /// <inheritdoc/>
    public sealed override uint Deserialize(string value, Type targetType)
    {
        return uint.Parse(value);
    }
}
