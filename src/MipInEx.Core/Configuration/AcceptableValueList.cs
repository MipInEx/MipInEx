using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MipInEx.Configuration;

/// <summary>
/// Specify the list of acceptable values for a setting.
/// </summary>
public interface IAcceptableValueList : IAcceptableValue
{
    /// <summary>
    /// List of values that a setting can take.
    /// </summary>
    IEnumerable AcceptableValues { get; }
}

/// <summary>
/// Specify the list of acceptable values for a setting.
/// </summary>
public abstract class AcceptableValueListBase : AcceptableValueBase, IAcceptableValueList
{
    /// <summary>
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    protected AcceptableValueListBase(Type valueType) : base(valueType)
    { }

    /// <inheritdoc/>
    public abstract IEnumerable AcceptableValues { get; }

    /// <inheritdoc/>
    public override object? Clamp(object? value)
    {
        object firstValue = null!;
        bool first = true;

        foreach (object v in this.AcceptableValues)
        {
            if (first)
            {
                firstValue = v;
            }

            if (v.Equals(value))
            {
                return true;
            }

            first = false;
        }

        return firstValue;
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        foreach (object v in this.AcceptableValues)
        {
            if (v.Equals(value))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public override string ToDescriptionString()
    {
        StringBuilder builder = new("# Acceptable values: ");

        bool first = true;
        foreach (object v in this.AcceptableValues)
        {
            if (!first)
                builder.Append(", ");

            builder.Append(v);
            first = true;
        }

        return builder.ToString();
    }
}

/// <summary>
/// Specify the list of acceptable values for a setting.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public abstract class AcceptableValueListBase<T> : AcceptableValueBase<T>, IAcceptableValueList
    where T : IEquatable<T>
{
    /// <inheritdoc cref="IAcceptableValueList.AcceptableValues"/>
    public abstract IReadOnlyList<T> AcceptableValues { get; }
    IEnumerable IAcceptableValueList.AcceptableValues => this.AcceptableValues;

    /// <inheritdoc/>
    public override T Clamp(T value)
    {
        if (this.IsValid(value)) return value;

        return this.AcceptableValues[0];
    }

    /// <inheritdoc/>
    public override bool IsValid(T value)
    {
        foreach (T v in this.AcceptableValues)
        {
            if (v.Equals(value))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public override string ToDescriptionString()
    {
        return $"# Acceptable values: {string.Join(", ", this.AcceptableValues.Select(x => x.ToString()))}";
    }
}

/// <summary>
/// Specify the list of acceptable values for a setting.
/// </summary>
public sealed class AcceptableValueList : AcceptableValueListBase
{
    private readonly ImmutableArray<object> acceptableValues;

    /// <summary>
    /// Initializes this value list.
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    /// <param name="acceptableValue">
    /// The acceptable value.
    /// </param>
    public AcceptableValueList(Type valueType, object acceptableValue)
        : base(valueType)
    {
        this.acceptableValues = ImmutableArray.Create(acceptableValue);
    }

    /// <summary>
    /// Initializes this value list.
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    /// <param name="acceptableValues">
    /// An array of acceptable value.
    /// </param>
    public AcceptableValueList(Type valueType, params object[] acceptableValues)
        : base(valueType)
    {
        this.acceptableValues = acceptableValues.Where(x => x is not null).ToImmutableArray();
    }

    /// <summary>
    /// Initializes this value list.
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    /// <param name="acceptableValues">
    /// An array of acceptable value.
    /// </param>
    public AcceptableValueList(Type valueType, Array acceptableValues)
        : base(valueType)
    {
        ImmutableArray<object>.Builder builder = ImmutableArray.CreateBuilder<object>(acceptableValues.Length);

        foreach (object value in acceptableValues)
        {
            builder.Add(value);
        }

        this.acceptableValues = builder.ToImmutable();
    }

    /// <summary>
    /// Initializes this value list.
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    /// <param name="acceptableValues">
    /// An enumerable of acceptable values.
    /// </param>
    public AcceptableValueList(Type valueType, IEnumerable<object> acceptableValues)
        : base(valueType)
    {
        this.acceptableValues = acceptableValues.Where(x => x is not null).ToImmutableArray();
    }

    /// <summary>
    /// Initializes this value list.
    /// </summary>
    /// <param name="valueType">
    /// Type of values that this class can Clamp.
    /// </param>
    /// <param name="acceptableValues">
    /// An enumerable of acceptable values.
    /// </param>
    public AcceptableValueList(Type valueType, IEnumerable acceptableValues)
        : base(valueType)
    {
        ImmutableArray<object>.Builder builder = ImmutableArray.CreateBuilder<object>();

        foreach (object value in acceptableValues)
        {
            builder.Add(value);
        }

        this.acceptableValues = builder.ToImmutable();
    }

    /// <inheritdoc/>
    public override IEnumerable AcceptableValues => this.acceptableValues;
}

/// <summary>
/// Specify the list of acceptable values for a setting.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public sealed class AcceptableValueList<T> : AcceptableValueListBase<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> acceptableValues;

    /// <summary>
    /// Initializes this value list.
    /// </summary>
    /// <param name="acceptableValue">
    /// The acceptable value.
    /// </param>
    public AcceptableValueList(T acceptableValue)
    {
        this.acceptableValues = ImmutableArray.Create(acceptableValue);
    }

    /// <summary>
    /// Initializes this value list.
    /// </summary>
    /// <param name="acceptableValues">
    /// An array of acceptable value.
    /// </param>
    public AcceptableValueList(params T[] acceptableValues)
    {
        this.acceptableValues = acceptableValues.Where(x => x is not null).ToImmutableArray();
    }

    /// <summary>
    /// Initializes this value list.
    /// </summary>
    /// <param name="acceptableValues">
    /// An enumerable of acceptable values.
    /// </param>
    public AcceptableValueList(IEnumerable<T> acceptableValues)
    {
        this.acceptableValues = acceptableValues.Where(x => x is not null).ToImmutableArray();
    }

    /// <inheritdoc/>
    public override IReadOnlyList<T> AcceptableValues => this.acceptableValues;
}
