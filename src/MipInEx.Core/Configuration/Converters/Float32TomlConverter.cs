using System;
using System.Globalization;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="float"/> Converter.
/// </summary>
public sealed class Float32TomlConverter : SpecializedTomlTypeConverter<float>
{
    /// <inheritdoc/>
    public sealed override string Serialize(float value, Type targetType)
    {
        return value.ToString(NumberFormatInfo.InvariantInfo);
    }

    /// <inheritdoc/>
    public sealed override float Deserialize(string value, Type targetType)
    {
        return float.Parse(value, NumberFormatInfo.InvariantInfo);
    }
}
