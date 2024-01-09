using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// An <see cref="Enum"/> Converter.
/// </summary>
public sealed class EnumTomlConverter : ITomlTypeConverter
{
    /// <inheritdoc/>
    public bool CanConvert(Type targetType)
    {
        return targetType.IsEnum;
    }

    /// <inheritdoc/>
    public string Serialize(object? value, Type targetType)
    {
        return value!.ToString();
    }

    /// <inheritdoc/>
    public object? Deserialize(string value, Type targetType)
    {
        return Enum.Parse(targetType, value, true);
    }
}
