using System;
using System.Globalization;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="decimal"/> Converter.
/// </summary>
public sealed class DecimalTomlConverter : SpecializedTomlTypeConverter<decimal>
{
    /// <inheritdoc/>
    public sealed override string Serialize(decimal value, Type targetType)
    {
        return value.ToString(NumberFormatInfo.InvariantInfo);
    }

    /// <inheritdoc/>
    public sealed override decimal Deserialize(string value, Type targetType)
    {
        return decimal.Parse(value, NumberFormatInfo.InvariantInfo);
    }
}
