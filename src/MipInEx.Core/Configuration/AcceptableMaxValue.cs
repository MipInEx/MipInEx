using System;

namespace MipInEx.Configuration;

/// <summary>
/// Specify a maximum value for a setting.
/// </summary>
public interface IAcceptableMaxValue : IAcceptableValueRangeLike
{
    /// <inheritdoc cref="IAcceptableValueRangeLike.MaxValue"/>
    new object MaxValue { get; }

    object? IAcceptableValueRangeLike.MinValue => null;

    bool IAcceptableValueRangeLike.HasMinValue => false;
    bool IAcceptableValueRangeLike.HasMaxValue => true;
}

/// <summary>
/// Specify a maximum value for a setting.
/// </summary>
public abstract class AcceptableMaxValueBase : AcceptableValueBase, IAcceptableMaxValue
{
    /// <summary>
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    protected AcceptableMaxValueBase(Type valueType) : base(valueType)
    { }

    /// <inheritdoc cref="IAcceptableMaxValue.MaxValue"/>
    public abstract IComparable MaxValue { get; }
    object IAcceptableMaxValue.MaxValue => this.MaxValue;
    object? IAcceptableValueRangeLike.MaxValue => this.MaxValue;

    /// <inheritdoc/>
    public override object? Clamp(object? value)
    {
        if (this.MaxValue.CompareTo(value) < 0)
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
        return this.MaxValue.CompareTo(value) >= 0;
    }

    /// <inheritdoc/>
    public override string ToDescriptionString()
    {
        return $"# Acceptable value range: Less than or equal to {this.MaxValue}";
    }
}

/// <summary>
/// Specify a maximum value for a setting.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public abstract class AcceptableMaxValueBase<T> : AcceptableValueBase<T>, IAcceptableMaxValue
    where T : notnull, IComparable<T>
{
    /// <inheritdoc cref="IAcceptableMaxValue.MaxValue"/>
    public abstract T MaxValue { get; }
    object IAcceptableMaxValue.MaxValue => this.MaxValue;
    object? IAcceptableValueRangeLike.MaxValue => this.MaxValue;

    /// <inheritdoc/>
    public override T Clamp(T value)
    {
        if (this.MaxValue.CompareTo(value) < 0)
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
        return this.MaxValue.CompareTo(value) >= 0;
    }

    /// <inheritdoc/>
    public override string ToDescriptionString()
    {
        return $"# Acceptable value range: Less than or equal to {this.MaxValue}";
    }
}

/// <summary>
/// Specify a maximum value for a setting.
/// </summary>
public sealed class AcceptableMaxValue : AcceptableMaxValueBase
{
    private readonly IComparable maxValue;

    /// <summary>
    /// Initializes this maximum.
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    /// <param name="maxValue">
    /// Highest acceptable value
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="maxValue"/> is
    /// <see langword="null"/>.
    /// </exception>
    public AcceptableMaxValue(Type valueType, IComparable maxValue)
        : base(valueType)
    {
        this.maxValue = maxValue ?? throw new ArgumentNullException(nameof(maxValue));
    }

    /// <inheritdoc/>
    public sealed override IComparable MaxValue => this.maxValue;
}

/// <summary>
/// Specify a maximum value for a setting.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public sealed class AcceptableMaxValue<T> : AcceptableMaxValueBase<T>
    where T : notnull, IComparable<T>
{
    private readonly T maxValue;

    /// <summary>
    /// Initializes this maximum.
    /// </summary>
    /// <param name="maxValue">
    /// Highest acceptable value
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="maxValue"/> is
    /// <see langword="null"/>.
    /// </exception>
    public AcceptableMaxValue(T maxValue)
    {
        this.maxValue = maxValue ?? throw new ArgumentNullException(nameof(maxValue));
    }

    /// <inheritdoc/>
    public sealed override T MaxValue => this.maxValue;
}