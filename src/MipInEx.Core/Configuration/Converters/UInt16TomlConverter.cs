using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// An <see cref="ushort"/> Converter.
/// </summary>
public sealed class UInt16TomlConverter : SpecializedTomlTypeConverter<ushort>
{
    /// <inheritdoc/>
    public sealed override string Serialize(ushort value, Type targetType)
    {
        return value.ToString();
    }

    /// <inheritdoc/>
    public sealed override ushort Deserialize(string value, Type targetType)
    {
        return ushort.Parse(value);
    }
}
