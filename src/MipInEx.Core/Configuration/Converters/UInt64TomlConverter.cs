using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// An <see cref="ulong"/> Converter.
/// </summary>
public sealed class UInt64TomlConverter : SpecializedTomlTypeConverter<ulong>
{
    /// <inheritdoc/>
    public sealed override string Serialize(ulong value, Type targetType)
    {
        return value.ToString();
    }

    /// <inheritdoc/>
    public sealed override ulong Deserialize(string value, Type targetType)
    {
        return ulong.Parse(value);
    }
}