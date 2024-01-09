using System;

namespace MipInEx.Configuration;

/// <summary>
/// A serializer/deserializer combo for some type(s). Used by
/// the config system.
/// </summary>
/// <typeparam name="T">
/// The type this converter converts.
/// </typeparam>
public abstract class TomlTypeConverter<T> : ITomlTypeConverter
{
    /// <inheritdoc cref="ITomlTypeConverter.CanConvert(Type)"/>
    public virtual bool CanConvert(Type targetType)
    {
        return targetType == typeof(T);
    }

    /// <summary>
    /// Serializes the type into a (hopefully) human-readable
    /// string.
    /// </summary>
    /// <param name="value">
    /// The instance to serialize.
    /// </param>
    /// <param name="targetType">
    /// The type of <paramref name="value"/>.
    /// </param>
    /// <returns>
    /// A string representation of <paramref name="value"/>.
    /// </returns>
    public abstract string Serialize(T? value, Type targetType);

    /// <summary>
    /// Deserializes the value from a string.
    /// </summary>
    /// <param name="value">
    /// The data to deserialize.
    /// </param>
    /// <param name="targetType">
    /// The type to deserialize to.
    /// </param>
    /// <returns>
    /// The deserialized instance of
    /// <paramref name="targetType"/>.
    /// </returns>
    public abstract T? Deserialize(string value, Type targetType);

    string ITomlTypeConverter.Serialize(object? value, Type targetType) => this.Serialize((T?)value, targetType);
    object? ITomlTypeConverter.Deserialize(string value, Type targetType) => this.Deserialize(value, targetType);
}

/// <summary>
/// A serializer/deserializer combo for some type(s). Used by
/// the config system.
/// </summary>
/// <typeparam name="T">
/// The type this converter converts.
/// </typeparam>
public abstract class SpecializedTomlTypeConverter<T> : ISpecializedTomlTypeConverter
{
    /// <inheritdoc/>
    public Type SpecializedType => typeof(T);

    /// <summary>
    /// Serializes the type into a (hopefully) human-readable
    /// string.
    /// </summary>
    /// <param name="value">
    /// The instance to serialize.
    /// </param>
    /// <param name="targetType">
    /// The type of <paramref name="value"/>.
    /// </param>
    /// <returns>
    /// A string representation of <paramref name="value"/>.
    /// </returns>
    public abstract string Serialize(T? value, Type targetType);

    /// <summary>
    /// Deserializes the value from a string.
    /// </summary>
    /// <param name="value">
    /// The data to deserialize.
    /// </param>
    /// <param name="targetType">
    /// The type to deserialize to.
    /// </param>
    /// <returns>
    /// The deserialized instance of
    /// <paramref name="targetType"/>.
    /// </returns>
    public abstract T? Deserialize(string value, Type targetType);

    string ITomlTypeConverter.Serialize(object? value, Type targetType) => this.Serialize((T?)value, targetType);
    object? ITomlTypeConverter.Deserialize(string value, Type targetType) => this.Deserialize(value, targetType);
}

/// <summary>
/// A serializer/deserializer combo for some type(s). Used by
/// the config system.
/// </summary>
public interface ISpecializedTomlTypeConverter : ITomlTypeConverter
{
    /// <summary>
    /// The type this converter is specialized for.
    /// </summary>
    Type SpecializedType { get; }

    bool ITomlTypeConverter.CanConvert(Type targetType)
    {
        return this.SpecializedType == targetType;
    }
}

/// <summary>
/// A serializer/deserializer combo for some type(s). Used by
/// the config system.
/// </summary>
public interface ITomlTypeConverter
{
    /// <summary>
    /// Returns whether or not this converter can convert the
    /// specified <paramref name="targetType"/>.
    /// </summary>
    /// <param name="targetType">
    /// The type to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this convert can convert it;
    /// <see langword="false"/> otherwise.
    /// </returns>
    bool CanConvert(Type targetType);

    /// <summary>
    /// Serializes the type into a (hopefully) human-readable
    /// string.
    /// </summary>
    /// <param name="value">
    /// The instance to serialize.
    /// </param>
    /// <param name="targetType">
    /// The type of <paramref name="value"/>.
    /// </param>
    /// <returns>
    /// A string representation of <paramref name="value"/>.
    /// </returns>
    string Serialize(object? value, Type targetType);

    /// <summary>
    /// Deserializes the value from a string.
    /// </summary>
    /// <param name="value">
    /// The data to deserialize.
    /// </param>
    /// <param name="targetType">
    /// The type to deserialize to.
    /// </param>
    /// <returns>
    /// The deserialized instance of
    /// <paramref name="targetType"/>.
    /// </returns>
    object? Deserialize(string value, Type targetType);
}
