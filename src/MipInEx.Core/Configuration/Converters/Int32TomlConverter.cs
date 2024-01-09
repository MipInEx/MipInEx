using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// An <see cref="int"/> Converter.
/// </summary>
public sealed class Int32TomlConverter : SpecializedTomlTypeConverter<int>
{
    /// <inheritdoc/>
    public sealed override string Serialize(int value, Type targetType)
    {
        return value.ToString();
    }

    /// <inheritdoc/>
    public sealed override int Deserialize(string value, Type targetType)
    {
        return int.Parse(value);
    }
}
