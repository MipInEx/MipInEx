using MipInEx.Configuration.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx.Configuration;

/// <summary>
/// Serializer/deserializer used by the config system.
/// </summary>
public static class TomlSerializer
{
    private readonly static Dictionary<Type, ISpecializedTomlTypeConverter> specializedTypeConverters = new()
    {
        [typeof(string)] = new StringTomlConverter(),
        [typeof(bool)] = new BooleanTomlConverter(),
        [typeof(byte)] = new ByteTomlConverter(),
        [typeof(sbyte)] = new SByteTomlConverter(),
        [typeof(short)] = new Int16TomlConverter(),
        [typeof(ushort)] = new UInt16TomlConverter(),
        [typeof(int)] = new Int32TomlConverter(),
        [typeof(uint)] = new UInt32TomlConverter(),
        [typeof(long)] = new Int64TomlConverter(),
        [typeof(ulong)] = new UInt64TomlConverter(),
        [typeof(float)] = new Float32TomlConverter(),
        [typeof(double)] = new Float64TomlConverter(),
        [typeof(decimal)] = new DecimalTomlConverter(),

        [typeof(bool?)] = new NullableTomlConverter<bool>(),
        [typeof(byte?)] = new NullableTomlConverter<byte>(),
        [typeof(sbyte?)] = new NullableTomlConverter<sbyte>(),
        [typeof(short?)] = new NullableTomlConverter<short>(),
        [typeof(ushort?)] = new NullableTomlConverter<ushort>(),
        [typeof(int?)] = new NullableTomlConverter<int>(),
        [typeof(uint?)] = new NullableTomlConverter<uint>(),
        [typeof(long?)] = new NullableTomlConverter<long>(),
        [typeof(ulong?)] = new NullableTomlConverter<ulong>(),
        [typeof(float?)] = new NullableTomlConverter<float>(),
        [typeof(double?)] = new NullableTomlConverter<double>(),
        [typeof(decimal?)] = new NullableTomlConverter<decimal>(),
    };

    private readonly static List<ITomlTypeConverter> typeConverters = new();
    private static readonly EnumTomlConverter enumTypeConverter = new();

    /// <summary>
    /// Serializes the object using the available converters.
    /// </summary>
    /// <param name="value">
    /// The value to convert.
    /// </param>
    /// <typeparam name="T">
    /// The type <paramref name="value"/> is.
    /// </typeparam>
    /// <returns>
    /// A serialized representation of
    /// <paramref name="value"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> has no converters.
    /// </exception>
    /// <exception cref="Exception">
    /// An error occurs during serialization.
    /// </exception>
    public static string Serialize<T>(T? value)
    {
        return TomlSerializer.Serialize(value, typeof(T));
    }

    /// <summary>
    /// Serializes the object using the available converters.
    /// </summary>
    /// <param name="value">
    /// The value to convert.
    /// </param>
    /// <param name="targetType">
    /// The type <paramref name="value"/> is.
    /// </param>
    /// <returns>
    /// A serialized representation of
    /// <paramref name="value"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="targetType"/> has no converters.
    /// </exception>
    /// <exception cref="Exception">
    /// An error occurs during serialization.
    /// </exception>
    public static string Serialize(object? value, Type targetType)
    {
        if (!TomlSerializer.TryGetTypeConverter(targetType, out ITomlTypeConverter? converter))
        {
            throw new InvalidOperationException($"Cannot convert from type {targetType}.");
        }

        return converter.Serialize(value, targetType);
    }

    /// <summary>
    /// Deserializes the string to the specified target type.
    /// </summary>
    /// <typeparam name="T">
    /// The type to deserialize <paramref name="value"/> to.
    /// </typeparam>
    /// <param name="value">
    /// The string value to deserialize.
    /// </param>
    /// <returns>
    /// An instance of <typeparamref name="T"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> has no converters.
    /// </exception>
    /// <exception cref="Exception">
    /// An error occurs during deserialization.
    /// </exception>
    public static T? Deserialize<T>(string value)
    {
        return (T?)TomlSerializer.Deserialize(value, typeof(T));
    }

    /// <summary>
    /// Deserializes the string to the specified target type.
    /// </summary>
    /// <param name="value">
    /// The string value to deserialize.
    /// </param>
    /// <param name="targetType">
    /// The type to deserialize <paramref name="value"/> to.
    /// </param>
    /// <returns>
    /// An instance of <paramref name="targetType"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="targetType"/> has no converters.
    /// </exception>
    /// <exception cref="Exception">
    /// An error occurs during deserialization.
    /// </exception>
    public static object? Deserialize(string value, Type targetType)
    {
        if (!TomlSerializer.TryGetTypeConverter(targetType, out ITomlTypeConverter? converter))
        {
            throw new InvalidOperationException($"Cannot convert from type {targetType}");
        }

        return converter.Deserialize(value, targetType);
    }

    /// <summary>
    /// Attempts to get the type converter for the specified
    /// type.
    /// </summary>
    /// <param name="type">
    /// The type to get the converter of.
    /// </param>
    /// <param name="converter">
    /// The found converter, <see langword="null"/> if not
    /// found.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the converter was found;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool TryGetTypeConverter([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out ITomlTypeConverter? converter)
    {
        if (type == null)
        {
            converter = null;
            return false;
        }

        if (TomlSerializer.specializedTypeConverters.TryGetValue(type, out ISpecializedTomlTypeConverter? specializedConverter))
        {
            converter = specializedConverter;
            return true;
        }

        foreach (ITomlTypeConverter typeConverter in TomlSerializer.typeConverters)
        {
            if (typeConverter.CanConvert(type))
            {
                converter = typeConverter;
                return true;
            }
        }

        if (type.IsEnum)
        {
            converter = TomlSerializer.enumTypeConverter;
            return true;
        }

        converter = null;
        return false;
    }

    /// <summary>
    /// Returns whether or not the specified type can be
    /// converted.
    /// </summary>
    /// <param name="targetType">The type to check.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="targetType"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static bool CanConvert(Type targetType)
    {
        if (targetType == null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        if (TomlSerializer.specializedTypeConverters.ContainsKey(targetType))
        {
            return true;
        }

        foreach (ITomlTypeConverter typeConverter in TomlSerializer.typeConverters)
        {
            if (typeConverter.CanConvert(targetType))
            {
                return true;
            }
        }

        return targetType.IsEnum;
    }

    /// <summary>
    /// Adds an instance of the specified converter type to the
    /// serializer.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the converter.
    /// </typeparam>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="T"/> is
    /// <see cref="ISpecializedTomlTypeConverter"/> and
    /// a converter for it's special type already exists.
    /// </exception>
    public static void AddConverter<T>()
        where T : ITomlTypeConverter, new()
    {
        T typeConverter = new();

        if (typeConverter is ISpecializedTomlTypeConverter specializedTypeConverter)
        {
            TomlSerializer.specializedTypeConverters.Add(specializedTypeConverter.SpecializedType, specializedTypeConverter);
        }
        else
        {
            TomlSerializer.typeConverters.Add(typeConverter);
        }
    }

    /// <summary>
    /// Adds the specified converter to the serializer.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the converter.
    /// </typeparam>
    /// <param name="typeConverter">
    /// The converter to add.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="typeConverter"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="typeConverter"/> is
    /// <see cref="ISpecializedTomlTypeConverter"/> and
    /// a converter for it's special type already exists.
    /// </exception>
    public static void AddConverter<T>(T typeConverter)
        where T : ITomlTypeConverter
    {
        if (typeConverter is null) throw new ArgumentNullException(nameof(typeConverter));

        if (typeConverter is ISpecializedTomlTypeConverter specializedTypeConverter)
        {
            TomlSerializer.specializedTypeConverters.Add(specializedTypeConverter.SpecializedType, specializedTypeConverter);
        }
        else
        {
            TomlSerializer.typeConverters.Add(typeConverter);
        }
    }
}
