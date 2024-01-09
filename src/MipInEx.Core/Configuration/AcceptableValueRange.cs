using System;

namespace MipInEx.Configuration;

/// <summary>
/// Specify a range-like restriction of acceptable values for a
/// setting.
/// </summary>
public interface IAcceptableValueRangeLike : IAcceptableValue
{
    /// <summary>
    /// Lowest acceptable value.
    /// </summary>
    object? MinValue { get; }

    /// <summary>
    /// Highest acceptable value.
    /// </summary>
    object? MaxValue { get; }

    /// <summary>
    /// Whether or not there is a lowest acceptable value.
    /// </summary>
    bool HasMinValue { get; }

    /// <summary>
    /// Whether or not there is a highest acceptable value.
    /// </summary>
    bool HasMaxValue { get; }
}

/// <summary>
/// Specify the range of acceptable values for a setting.
/// </summary>
public interface IAcceptableValueRange : IAcceptableValue, IAcceptableValueRangeLike
{
    /// <inheritdoc cref="IAcceptableValueRangeLike.MinValue"/>
    new object MinValue { get; }
    /// <inheritdoc cref="IAcceptableValueRangeLike.MaxValue"/>
    new object MaxValue { get; }

    bool IAcceptableValueRangeLike.HasMinValue => true;
    bool IAcceptableValueRangeLike.HasMaxValue => true;
}

/// <summary>
/// Specify the range of acceptable values for a setting.
/// </summary>
public abstract class AcceptableValueRangeBase : AcceptableValueBase, IAcceptableValueRange
{
    /// <summary>
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    protected AcceptableValueRangeBase(Type valueType) : base(valueType)
    { }

    /// <inheritdoc cref="IAcceptableValueRange.MinValue"/>
    public abstract IComparable MinValue { get; }
    object IAcceptableValueRange.MinValue => this.MinValue;
    object? IAcceptableValueRangeLike.MinValue => this.MinValue;

    /// <inheritdoc cref="IAcceptableValueRange.MaxValue"/>
    public abstract IComparable MaxValue { get; }
    object IAcceptableValueRange.MaxValue => this.MaxValue;
    object? IAcceptableValueRangeLike.MaxValue => this.MaxValue;

    /// <inheritdoc/>
    public override object? Clamp(object? value)
    {
        if (this.MinValue.CompareTo(value) > 0)
        {
            return this.MinValue;
        }
        else if (this.MaxValue.CompareTo(value) < 0)
        {
            return this.MaxValue;
        }
        else
        {
            return value;
        }
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        return this.MinValue.CompareTo(value) <= 0 && this.MaxValue.CompareTo(value) >= 0;
    }

    /// <inheritdoc/>
    public override string ToDescriptionString()
    {
        return $"# Acceptable value range: From {this.MinValue} to {this.MaxValue}";
    }
}

/// <summary>
/// Specify the range of acceptable values for a setting.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public abstract class AcceptableValueRangeBase<T> : AcceptableValueBase<T>, IAcceptableValueRange
    where T : notnull, IComparable<T>
{

    /// <inheritdoc cref="IAcceptableValueRange.MinValue"/>
    public abstract T MinValue { get; }
    object IAcceptableValueRange.MinValue => this.MinValue;
    object? IAcceptableValueRangeLike.MinValue => this.MinValue;

    /// <inheritdoc cref="IAcceptableValueRange.MaxValue"/>
    public abstract T MaxValue { get; }
    object IAcceptableValueRange.MaxValue => this.MaxValue;
    object? IAcceptableValueRangeLike.MaxValue => this.MaxValue;

    /// <inheritdoc/>
    public override T Clamp(T value)
    {
        if (this.MinValue.CompareTo(value) > 0)
        {
            return this.MinValue;
        }
        else if (this.MaxValue.CompareTo(value) < 0)
        {
            return this.MaxValue;
        }
        else
        {
            return value;
        }
    }

    /// <inheritdoc/>
    public override bool IsValid(T value)
    {
        return this.MinValue.CompareTo(value) <= 0 && this.MaxValue.CompareTo(value) >= 0;
    }

    /// <inheritdoc/>
    public override string ToDescriptionString()
    {
        return $"# Acceptable value range: From {this.MinValue} to {this.MaxValue}";
    }
}

/// <summary>
/// Specify the range of acceptable values for a setting.
/// </summary>
public sealed class AcceptableValueRange : AcceptableValueRangeBase
{
    private readonly IComparable minValue;
    private readonly IComparable maxValue;

    /// <summary>
    /// Initializes this range.
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    /// <param name="minValue">
    /// Lowest acceptable value
    /// </param>
    /// <param name="maxValue">
    /// Highest acceptable value
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="minValue"/> or
    /// <paramref name="maxValue"/> are
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="minValue"/> is greater than or equal to
    /// <paramref name="maxValue"/>.
    /// </exception>
    public AcceptableValueRange(Type valueType, IComparable minValue, IComparable maxValue)
        : base(valueType)
    {
        this.minValue = minValue ?? throw new ArgumentNullException(nameof(minValue));
        this.maxValue = maxValue ?? throw new ArgumentNullException(nameof(maxValue));

        if (minValue.CompareTo(maxValue) >= 0)
        {
            throw new ArgumentException($"{nameof(minValue)} has to be lower than {nameof(maxValue)}");
        }
    }

    /// <inheritdoc/>
    public sealed override IComparable MinValue => this.minValue;

    /// <inheritdoc/>
    public sealed override IComparable MaxValue => this.maxValue;
}

/// <summary>
/// Specify the range of acceptable values for a setting.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public sealed class AcceptableValueRange<T> : AcceptableValueRangeBase<T>
    where T : notnull, IComparable<T>
{
    private readonly T minValue;
    private readonly T maxValue;

    /// <summary>
    /// Initializes this range.
    /// </summary>
    /// <param name="minValue">
    /// Lowest acceptable value
    /// </param>
    /// <param name="maxValue">
    /// Highest acceptable value
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="minValue"/> or
    /// <paramref name="maxValue"/> are
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="minValue"/> is greater than or equal to
    /// <paramref name="maxValue"/>.
    /// </exception>
    public AcceptableValueRange(T minValue, T maxValue)
    {
        this.minValue = minValue ?? throw new ArgumentNullException(nameof(minValue));
        this.maxValue = maxValue ?? throw new ArgumentNullException(nameof(maxValue));

        if (minValue.CompareTo(maxValue) >= 0)
        {
            throw new ArgumentException($"{nameof(minValue)} has to be lower than {nameof(maxValue)}");
        }
    }

    /// <inheritdoc/>
    public sealed override T MinValue => this.minValue;

    /// <inheritdoc/>
    public sealed override T MaxValue => this.maxValue;
}
