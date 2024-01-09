using System;

namespace MipInEx.Logging;

/// <summary>
/// Log event arguments. Contains info about the log message.
/// </summary>
public class LogEventArgs : EventArgs
{
    private readonly object? data;
    private readonly LogLevel level;
    private readonly ILogSource source;

    /// <summary>
    /// Creates the log event args
    /// </summary>
    /// <param name="data">
    /// Logged data.
    /// </param>
    /// <param name="level">
    /// Log level of the data.
    /// </param>
    /// <param name="source">
    /// Log source that emits these args.
    /// </param>
    public LogEventArgs(object? data, LogLevel level, ILogSource source)
    {
        this.data = data;
        this.level = level;
        this.source = source;
    }

    /// <summary>
    /// Logged data.
    /// </summary>
    public object? Data => this.data;

    /// <summary>
    /// Log levels for the data.
    /// </summary>
    public LogLevel Level => this.level;

    /// <summary>
    /// Log source that emitted the log event.
    /// </summary>
    public ILogSource Source => this.source;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[{this.level,-7}:{this.source.SourceName,10}] {this.data}";
    }

    /// <summary>
    /// Like <see cref="ToString"/> but appends newline at the
    /// end.
    /// </summary>
    /// <returns>
    /// Same output as <see cref="ToString"/> but with new
    /// line.
    /// </returns>
    public string ToStringLine()
    {
        return $"[{this.level,-7}:{this.source.SourceName,10}] {this.data}{Environment.NewLine}";
    }
}
