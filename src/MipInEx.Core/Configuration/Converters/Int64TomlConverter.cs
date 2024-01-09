using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="long"/> Converter.
/// </summary>
public sealed class Int64TomlConverter : SpecializedTomlTypeConverter<long>
{
    /// <inheritdoc/>
    public sealed override string Serialize(long value, Type targetType)
    {
        return value.ToString();
    }

    /// <inheritdoc/>
    public sealed override long Deserialize(string value, Type targetType)
    {
        return long.Parse(value);
    }
}
