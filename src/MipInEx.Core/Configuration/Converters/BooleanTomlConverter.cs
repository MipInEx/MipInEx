using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="bool"/> Converter.
/// </summary>
public sealed class BooleanTomlConverter : SpecializedTomlTypeConverter<bool>
{
    /// <inheritdoc/>
    public sealed override string Serialize(bool value, Type targetType)
    {
        return value.ToString().ToLowerInvariant();
    }

    /// <inheritdoc/>
    public sealed override bool Deserialize(string value, Type targetType)
    {
        return bool.Parse(value);
    }
}
