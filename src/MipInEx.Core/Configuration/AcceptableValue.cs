using System;

namespace MipInEx.Configuration;

/// <summary>
/// Base type of all classes representing and enforcing
/// acceptable values of config settings.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public abstract class AcceptableValueBase<T> : IAcceptableValue
{
    /// <inheritdoc/>
    public Type ValueType => typeof(T);

    /// <inheritdoc cref="IAcceptableValue.Clamp(object?)"/>
    public abstract T Clamp(T value);

    object? IAcceptableValue.Clamp(object? value)
        => this.Clamp((T)value!);

    /// <inheritdoc cref="IAcceptableValue.IsValid(object?)"/>
    public abstract bool IsValid(T value);

    bool IAcceptableValue.IsValid(object? value)
        => value is T tValue && this.IsValid(tValue);

    /// <inheritdoc/>
    public abstract string ToDescriptionString();
}

/// <summary>
/// Base type of all classes representing and enforcing
/// acceptable values of config settings.
/// </summary>
public abstract class AcceptableValueBase : IAcceptableValue
{
    private readonly Type valueType;

    /// <summary></summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    protected AcceptableValueBase(Type valueType)
    {
        this.valueType = valueType;
    }

    /// <inheritdoc/>
    public Type ValueType => this.valueType;

    /// <inheritdoc/>
    public abstract object? Clamp(object? value);

    /// <inheritdoc/>
    public abstract bool IsValid(object? value);

    /// <inheritdoc/>
    public abstract string ToDescriptionString();
}

/// <summary>
/// Base interface of all types representing and enforcing
/// acceptable values of config settings.
/// </summary>
public interface IAcceptableValue
{
    /// <summary>
    /// Type of the supported values.
    /// </summary>
    Type ValueType { get; }

    /// <summary>
    /// Change the value to be acceptable, if it's not already.
    /// </summary>
    /// <param name="value">
    /// The value to check.
    /// </param>
    /// <returns>
    /// The clamped value.
    /// </returns>
    object? Clamp(object? value);

    /// <summary>
    /// Checks if the value is an acceptable value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> is
    /// valid; <see langword="false"/> otherwise.
    /// </returns>
    bool IsValid(object? value);

    /// <summary>
    /// Get the string for use in config files.
    /// </summary>
    /// <returns>
    /// The description string for use in config files.
    /// </returns>
    string ToDescriptionString();
}
