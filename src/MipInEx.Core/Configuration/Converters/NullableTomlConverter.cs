using System;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="Nullable{T}"/> Converter.
/// </summary>
/// <typeparam name="T">The type of nullable struct</typeparam>
public sealed class NullableTomlConverter<T> : SpecializedTomlTypeConverter<T?>
    where T : struct
{
    private ITomlTypeConverter? underlyingConverter;

    private ITomlTypeConverter UnderlyingConverter
    {
        get
        {
            if (this.underlyingConverter is null && !TomlSerializer.TryGetTypeConverter(typeof(T), out this.underlyingConverter))
            {
                throw new InvalidOperationException($"Converting to {typeof(T)} is not supported.");
            }

            return this.underlyingConverter;
        }
    }

    /// <inheritdoc/>
    public sealed override string Serialize(T? value, Type targetType)
    {
        if (value.HasValue)
        {
            return this.UnderlyingConverter.Serialize(value.Value, targetType);
        }

        return "Null";
    }

    /// <inheritdoc/>
    public sealed override T? Deserialize(string value, Type targetType)
    {
        if (string.Equals("Null", value, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        else
        {
            return (T)this.UnderlyingConverter.Deserialize(value, targetType)!;
        }
    }
}
