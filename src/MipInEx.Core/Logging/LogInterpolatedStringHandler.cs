using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace MipInEx.Logging.Interpolation;

/// <summary>
/// Interpolated string handler for <see cref="Logger"/>. This
/// allows to conditionally skip logging certain messages and
/// speed up logging in certain places.
/// </summary>
/// <remarks>
/// The class isn't meant to be constructed manually. Instead,
/// use
/// <see cref="ManualLogSource.Log(LogLevel, LogInterpolatedStringHandler)"/>
/// with string interpolation.
/// </remarks>
[InterpolatedStringHandler]
public class LogInterpolatedStringHandler
{
    // See
    // https://source.dot.net/#System.Private.CoreLib/DefaultInterpolatedStringHandler.cs,29
    private const int GUESSED_LENGTH_PER_HOLE = 11;

    // We can't use an array pool to support net35 builds, so
    // default to StringBuilder
    private readonly StringBuilder? backingBuilder;

    private readonly bool isEnabled;

    /// <summary>
    /// Constructs a log handler.
    /// </summary>
    /// <param name="literalLength">
    /// Length of the literal string.
    /// </param>
    /// <param name="formattedCount">
    /// Number for formatted items.
    /// </param>
    /// <param name="logLevel">
    /// Log level the message belongs to.
    /// </param>
    /// <param name="isEnabled">
    /// Whether this string should be logged.
    /// </param>
    public LogInterpolatedStringHandler(int literalLength, int formattedCount, LogLevel logLevel, out bool isEnabled)
    {
        this.isEnabled = (logLevel & Logger.ListenedLogLevels) != LogLevel.None;
        isEnabled = this.isEnabled;
        this.backingBuilder = this.isEnabled ?
            new StringBuilder(literalLength + (formattedCount * GUESSED_LENGTH_PER_HOLE)) :
            null;
    }

    /// <summary>
    /// Whether the interpolation is enabled and string will be
    /// logged.
    /// </summary>
    public bool Enabled => this.isEnabled;

    /// <summary>
    /// Appends a literal string to the interpolation.
    /// </summary>
    /// <param name="s">String to append.</param>
    public void AppendLiteral(string s)
    {
        if (!this.isEnabled) return;

        this.backingBuilder!.Append(s);
    }

    /// <summary>
    /// Appends a value to the interpolation.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the value to append.
    /// </typeparam>
    /// <param name="t">Value to append.</param>
    public void AppendFormatted<T>(T t)
    {
        if (!this.isEnabled) return;

        this.backingBuilder!.Append(t);
    }

    /// <summary>
    /// Append a formattable item.
    /// </summary>
    /// <typeparam name="T">
    /// Item type.
    /// </typeparam>
    /// <param name="t">Item to append.</param>
    /// <param name="format">Format to append with.</param>
    public void AppendFormatted<T>(T t, string format)
        where T : IFormattable
    {
        if (!this.isEnabled) return;

        this.backingBuilder!.Append(t?.ToString(format, null));
    }

    /// <summary>
    /// Append an IntPtr.
    /// </summary>
    /// <typeparam name="T">
    /// Item type.
    /// </typeparam>
    /// <param name="t">Item to append.</param>
    /// <param name="format">Format to append with.</param>
    public void AppendFormatted(IntPtr t, string format)
    {
        if (!this.isEnabled) return;

        this.backingBuilder!.Append(t.ToString(format));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return this.backingBuilder?.ToString() ?? string.Empty;
    }
}

/// <inheritdoc/>
[InterpolatedStringHandler]
public class FatalLogInterpolatedStringHandler : LogInterpolatedStringHandler
{
    /// <inheritdoc/>
    public FatalLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
        : base(literalLength, formattedCount, LogLevel.Fatal, out isEnabled)
    { }
}

/// <inheritdoc/>
[InterpolatedStringHandler]
public class ErrorLogInterpolatedStringHandler : LogInterpolatedStringHandler
{
    /// <inheritdoc/>
    public ErrorLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
        : base(literalLength, formattedCount, LogLevel.Error, out isEnabled)
    { }
}

/// <inheritdoc/>
[InterpolatedStringHandler]
public class WarningLogInterpolatedStringHandler : LogInterpolatedStringHandler
{
    /// <inheritdoc/>
    public WarningLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
        : base(literalLength, formattedCount, LogLevel.Warning, out isEnabled)
    { }
}

/// <inheritdoc/>
[InterpolatedStringHandler]
public class MessageLogInterpolatedStringHandler : LogInterpolatedStringHandler
{
    /// <inheritdoc/>
    public MessageLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
        : base(literalLength, formattedCount, LogLevel.Message, out isEnabled)
    { }
}

/// <inheritdoc/>
[InterpolatedStringHandler]
public class InfoLogInterpolatedStringHandler : LogInterpolatedStringHandler
{
    /// <inheritdoc/>
    public InfoLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
        : base(literalLength, formattedCount, LogLevel.Info, out isEnabled)
    { }
}

/// <inheritdoc/>
[InterpolatedStringHandler]
public class DebugLogInterpolatedStringHandler : LogInterpolatedStringHandler
{
    /// <inheritdoc/>
    public DebugLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
        : base(literalLength, formattedCount, LogLevel.Debug, out isEnabled)
    { }
}