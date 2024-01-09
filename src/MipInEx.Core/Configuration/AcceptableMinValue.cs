using System;

namespace MipInEx.Configuration;

/// <summary>
/// Specify a minimum value for a setting.
/// </summary>
public interface IAcceptableMinValue : IAcceptableValueRangeLike
{
    /// <inheritdoc cref="IAcceptableValueRangeLike.MinValue"/>
    new object MinValue { get; }

    object? IAcceptableValueRangeLike.MaxValue => null;

    bool IAcceptableValueRangeLike.HasMinValue => true;
    bool IAcceptableValueRangeLike.HasMaxValue => false;
}

/// <summary>
/// Specify a minimum value for a setting.
/// </summary>
public abstract class AcceptableMinValueBase : AcceptableValueBase, IAcceptableMinValue
{
    /// <summary>
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    protected AcceptableMinValueBase(Type valueType) : base(valueType)
    { }

    /// <inheritdoc cref="IAcceptableMinValue.MinValue"/>
    public abstract IComparable MinValue { get; }
    object IAcceptableMinValue.MinValue => this.MinValue;
    object? IAcceptableValueRangeLike.MinValue => this.MinValue;

    /// <inheritdoc/>
    public override object? Clamp(object? value)
    {
        if (this.MinValue.CompareTo(value) > 0)
        {
            return this.MinValue;
        }
        else
        {
            return value;
        }
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        return this.MinValue.CompareTo(value) <= 0;
    }

    /// <inheritdoc/>
    public override string ToDescriptionString()
    {
        return $"# Acceptable value range: Greater than or equal to {this.MinValue}";
    }
}

/// <summary>
/// Specify a minimum value for a setting.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public abstract class AcceptableMinValueBase<T> : AcceptableValueBase<T>, IAcceptableMinValue
    where T : notnull, IComparable<T>
{
    /// <inheritdoc cref="IAcceptableMinValue.MinValue"/>
    public abstract T MinValue { get; }
    object IAcceptableMinValue.MinValue => this.MinValue;
    object? IAcceptableValueRangeLike.MinValue => this.MinValue;

    /// <inheritdoc/>
    public override T Clamp(T value)
    {
        if (this.MinValue.CompareTo(value) > 0)
        {
            return this.MinValue;
        }
        else
        {
            return value;
        }
    }

    /// <inheritdoc/>
    public override bool IsValid(T value)
    {
        return this.MinValue.CompareTo(value) <= 0;
    }

    /// <inheritdoc/>
    public override string ToDescriptionString()
    {
        return $"# Acceptable value range: Greater than or equal to {this.MinValue}";
    }
}

/// <summary>
/// Specify a minimum value for a setting.
/// </summary>
public sealed class AcceptableMinValue : AcceptableMinValueBase
{
    private readonly IComparable minValue;

    /// <summary>
    /// Initializes this minimum.
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    /// <param name="minValue">
    /// Lowest acceptable value
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="minValue"/> is
    /// <see langword="null"/>.
    /// </exception>
    public AcceptableMinValue(Type valueType, IComparable minValue)
        : base(valueType)
    {
        this.minValue = minValue ?? throw new ArgumentNullException(nameof(minValue));
    }

    /// <inheritdoc/>
    public sealed override IComparable MinValue => this.minValue;
}

/// <summary>
/// Specify a minimum value for a setting.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public sealed class AcceptableMinValue<T> : AcceptableMinValueBase<T>
    where T : notnull, IComparable<T>
{
    private readonly T minValue;

    /// <summary>
    /// Initializes this minimum.
    /// </summary>
    /// <param name="minValue">
    /// Lowest acceptable value
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="minValue"/> is
    /// <see langword="null"/>.
    /// </exception>
    public AcceptableMinValue(T minValue)
    {
        this.minValue = minValue ?? throw new ArgumentNullException(nameof(minValue));
    }

    /// <inheritdoc/>
    public sealed override T MinValue => this.minValue;
}
