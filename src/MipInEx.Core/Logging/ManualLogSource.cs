using MipInEx.Logging.Interpolation;
using System;
using System.Runtime.CompilerServices;

namespace MipInEx.Logging;

/// <summary>
/// A generic, multi-purpose log source. Exposes simple API to
/// manually emit logs.
/// </summary>
public class ManualLogSource : ILogSource
{
    private readonly string sourceName;

    /// <summary>
    /// Creates a manual log source.
    /// </summary>
    /// <param name="sourceName">
    /// Name of the log source.
    /// </param>
    public ManualLogSource(string sourceName)
    {
        this.sourceName = sourceName;
    }

    /// <inheritdoc/>
    public string SourceName => this.sourceName;

    /// <inheritdoc/>
    public event EventHandler<LogEventArgs>? LogEvent;

    /// <inheritdoc/>
    public void Dispose() { }

    /// <summary>
    /// Logs a message with the specified log level.
    /// </summary>
    /// <param name="level">
    /// Log levels to attach to the message. Multiple can be
    /// used with bitwise ORing.
    /// </param>
    /// <param name="data">
    /// Data to log.
    /// </param>
    public void Log(LogLevel level, object? data)
    {
        this.LogEvent?.Invoke(this, new LogEventArgs(data, level, this));
    }

    /// <summary>
    /// Logs an interpolated string with the specified log
    /// level.
    /// </summary>
    /// <param name="level">
    /// Log levels to attach to the message. Multiple can be
    /// used with bitwise ORing.
    /// </param>
    /// <param name="logHandler">
    /// Handler for the interpolated string.
    /// </param>
    public void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler logHandler)
    {
        if (logHandler.Enabled)
            this.LogEvent?.Invoke(this, new LogEventArgs(logHandler.ToString(), level, this));
    }

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Fatal"/> level.
    /// </summary>
    /// <param name="data">Data to log.</param>
    public void LogFatal(object? data)
        => this.Log(LogLevel.Fatal, data);

    /// <summary>
    /// Logs an interpolated string with
    /// <see cref="LogLevel.Fatal"/> level.
    /// </summary>
    /// <param name="logHandler">
    /// Handler for the interpolated string.
    /// </param>
    public void LogFatal(FatalLogInterpolatedStringHandler logHandler)
        => this.Log(LogLevel.Fatal, logHandler);

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Error"/> level.
    /// </summary>
    /// <param name="data">Data to log.</param>
    public void LogError(object? data)
        => this.Log(LogLevel.Error, data);

    /// <summary>
    /// Logs an interpolated string with
    /// <see cref="LogLevel.Error"/> level.
    /// </summary>
    /// <param name="logHandler">
    /// Handler for the interpolated string.
    /// </param>
    public void LogError(ErrorLogInterpolatedStringHandler logHandler)
        => this.Log(LogLevel.Error, logHandler);

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Warning"/>
    /// level.
    /// </summary>
    /// <param name="data">Data to log.</param>
    public void LogWarning(object? data)
        => this.Log(LogLevel.Warning, data);

    /// <summary>
    /// Logs an interpolated string with
    /// <see cref="LogLevel.Warning"/> level.
    /// </summary>
    /// <param name="logHandler">
    /// Handler for the interpolated string.
    /// </param>
    public void LogWarning(WarningLogInterpolatedStringHandler logHandler)
        => this.Log(LogLevel.Warning, logHandler);

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Message"/>
    /// level.
    /// </summary>
    /// <param name="data">Data to log.</param>
    public void LogMessage(object? data)
        => this.Log(LogLevel.Message, data);

    /// <summary>
    /// Logs an interpolated string with
    /// <see cref="LogLevel.Message"/> level.
    /// </summary>
    /// <param name="logHandler">
    /// Handler for the interpolated string.
    /// </param>
    public void LogMessage(MessageLogInterpolatedStringHandler logHandler)
        => this.Log(LogLevel.Message, logHandler);

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Info"/> level.
    /// </summary>
    /// <param name="data">Data to log.</param>
    public void LogInfo(object? data)
        => this.Log(LogLevel.Info, data);

    /// <summary>
    /// Logs an interpolated string with
    /// <see cref="LogLevel.Info"/> level.
    /// </summary>
    /// <param name="logHandler">
    /// Handler for the interpolated string.
    /// </param>
    public void LogInfo(InfoLogInterpolatedStringHandler logHandler)
        => this.Log(LogLevel.Info, logHandler);

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Debug"/> level.
    /// </summary>
    /// <param name="data">Data to log.</param>
    public void LogDebug(object? data)
        => this.Log(LogLevel.Debug, data);

    /// <summary>
    /// Logs an interpolated string with
    /// <see cref="LogLevel.Debug"/> level.
    /// </summary>
    /// <param name="logHandler">
    /// Handler for the interpolated string.
    /// </param>
    public void LogDebug(DebugLogInterpolatedStringHandler logHandler)
        => this.Log(LogLevel.Debug, logHandler);
}
