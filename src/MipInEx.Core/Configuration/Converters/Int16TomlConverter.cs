using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="short"/> Converter.
/// </summary>
public sealed class Int16TomlConverter : SpecializedTomlTypeConverter<short>
{
    /// <inheritdoc/>
    public sealed override string Serialize(short value, Type targetType)
    {
        return value.ToString();
    }

    /// <inheritdoc/>
    public sealed override short Deserialize(string value, Type targetType)
    {
        return short.Parse(value);
    }
}
