using System;
using System.Globalization;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="double"/> Converter.
/// </summary>
public sealed class Float64TomlConverter : SpecializedTomlTypeConverter<double>
{
    /// <inheritdoc/>
    public sealed override string Serialize(double value, Type targetType)
    {
        return value.ToString(NumberFormatInfo.InvariantInfo);
    }

    /// <inheritdoc/>
    public sealed override double Deserialize(string value, Type targetType)
    {
        return double.Parse(value, NumberFormatInfo.InvariantInfo);
    }
}
