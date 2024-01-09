using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="sbyte"/> Converter.
/// </summary>
public sealed class SByteTomlConverter : SpecializedTomlTypeConverter<sbyte>
{
    /// <inheritdoc/>
    public sealed override string Serialize(sbyte value, Type targetType)
    {
        return value.ToString();
    }

    /// <inheritdoc/>
    public sealed override sbyte Deserialize(string value, Type targetType)
    {
        return sbyte.Parse(value);
    }
}
