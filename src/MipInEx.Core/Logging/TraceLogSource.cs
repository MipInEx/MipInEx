﻿using System.Diagnostics;

namespace MipInEx.Logging;

/// <summary>
/// A source that routes all logs from the inbuilt .NET
/// <see cref="Trace"/> API to the logging system.
/// </summary>
/// <inheritdoc cref="TraceListener"/>
public class TraceLogSource : TraceListener
{
    /// <summary>
    /// Internal log source.
    /// </summary>
    protected readonly ManualLogSource logSource;

    /// <summary>
    /// Creates a new trace log source.
    /// </summary>
    protected TraceLogSource()
    {
        this.logSource = new ManualLogSource("Trace");
    }

    private static TraceLogSource? traceListener;
    private static bool isListening;

    /// <summary>
    /// Whether Trace logs are currently being rerouted.
    /// </summary>
    public static bool IsListening => TraceLogSource.isListening;

    /// <summary>
    /// Creates a new trace log source.
    /// </summary>
    /// <returns>
    /// New log source (or already existing one).
    /// </returns>
    public static ILogSource CreateSource()
    {
        if (TraceLogSource.traceListener == null)
        {
            TraceLogSource.traceListener = new TraceLogSource();
            Trace.Listeners.Add(TraceLogSource.traceListener);
            TraceLogSource.isListening = true;
        }

        return TraceLogSource.traceListener.logSource;
    }

    /// <summary>
    /// Writes a message to the underlying
    /// <see cref="ManualLogSource"/> instance.
    /// </summary>
    /// <param name="message">
    /// The message to write.
    /// </param>
    public override void Write(string message)
    {
        this.logSource.Log(LogLevel.Info, message);
    }

    /// <summary>
    /// Writes a message and a newline to the underlying
    /// <see cref="ManualLogSource"/> instance.
    /// </summary>
    /// <param name="message">
    /// The message to write.
    /// </param>
    public override void WriteLine(string message)
    {
        this.logSource.Log(LogLevel.Info, message);
    }

    /// <inheritdoc/>
    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object?[] args)
    {
        this.TraceEvent(eventCache, source, eventType, id, string.Format(format, args));
    }

    /// <inheritdoc/>
    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
    {
        LogLevel level = eventType switch
        {
            TraceEventType.Critical => LogLevel.Fatal,
            TraceEventType.Error => LogLevel.Error,
            TraceEventType.Warning => LogLevel.Warning,
            TraceEventType.Information => LogLevel.Info,
            _ => LogLevel.Debug
        };

        this.logSource.Log(level, $"{message}".Trim());
    }
}
