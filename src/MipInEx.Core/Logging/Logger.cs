using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MipInEx.Logging.Interpolation;

namespace MipInEx.Logging;

/// <summary>
/// Handles pub-sub event marshalling across all log listeners
/// and sources.
/// </summary>
public static class Logger
{
    private static readonly ManualLogSource internalLogSource;
    private static readonly LogListenerCollection listeners;
    private static readonly LogSourceCollection sources;

    static Logger()
    {
        Logger.sources = new();
        Logger.listeners = new();

        // todo: find better name
        Logger.internalLogSource = Logger.CreateLogSource("Sandbox");
    }

    /// <summary>
    /// Log levels that are currently listened to by at least
    /// one listener.
    /// </summary>
    public static LogLevel ListenedLogLevels
        => Logger.listeners.activeLogLevels;

    /// <summary>
    /// Collection of all log listeners that receive log
    /// events.
    /// </summary>
    public static ICollection<ILogListener> Listeners => Logger.listeners;

    /// <summary>
    /// Collection of all log source that output log events.
    /// </summary>
    public static ICollection<ILogSource> Sources => Logger.sources;

    internal static void InternalLogEvent(object sender, LogEventArgs eventArgs)
    {
        foreach (ILogListener listener in Logger.listeners)
        {
            if ((eventArgs.Level & listener.LogLevelFilter) != LogLevel.None)
            {
                listener.LogEvent(sender, eventArgs);
            }
        }
    }

    /// <summary>
    /// Logs an entry to the internal logger instance.
    /// </summary>
    /// <param name="level">
    /// The level of the entry.
    /// </param>
    /// <param name="data">
    /// The data of the entry.
    /// </param>
    internal static void Log(LogLevel level, object? data)
        => Logger.internalLogSource.Log(level, data);

    /// <summary>
    /// Logs an entry to the internal logger instance if any
    /// log listener wants the message.
    /// </summary>
    /// <param name="level">
    /// The level of the entry.
    /// </param>
    /// <param name="logHandler">
    /// Log handler to resolve log from.
    /// </param>
    internal static void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler logHandler)
        => Logger.internalLogSource.Log(level, logHandler);

    /// <summary>
    /// Creates a new log source with a name and attaches it to
    /// <see cref="Sources"/>.
    /// </summary>
    /// <param name="sourceName">
    /// Name of the log source to create.
    /// </param>
    /// <returns>
    /// An instance of <see cref="ManualLogSource"/> that allows
    /// to write logs.
    /// </returns>
    public static ManualLogSource CreateLogSource(string sourceName)
    {
        ManualLogSource source = new(sourceName);
        Logger.sources.Add(source);
        return source;
    }

    private sealed class LogListenerCollection : 
        ICollection<ILogListener>,
        ICollection,
        IReadOnlyCollection<ILogListener>,
        IEnumerable<ILogListener>,
        IEnumerable
    {
        private readonly List<ILogListener> backingCollection;
        public LogLevel activeLogLevels;

        public LogListenerCollection()
        {
            this.backingCollection = new();
            this.activeLogLevels = LogLevel.None;
        }

        public int Count => this.backingCollection.Count;

        bool ICollection<ILogListener>.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        public void Add(ILogListener item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            this.activeLogLevels |= item.LogLevelFilter;

            this.backingCollection.Add(item);
        }

        public bool Contains([NotNullWhen(true)] ILogListener? item)
        {
            return this.backingCollection.Contains(item!);
        }

        public void CopyTo(ILogListener[] array, int arrayIndex)
        {
            this.backingCollection.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.backingCollection).CopyTo(array, index);
        }

        public void Clear()
        {
            this.activeLogLevels = LogLevel.None;
            this.backingCollection.Clear();
        }

        public List<ILogListener>.Enumerator GetEnumerator()
        {
            return this.backingCollection.GetEnumerator();
        }

        IEnumerator<ILogListener> IEnumerable<ILogListener>.GetEnumerator()
        {
            return this.backingCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.backingCollection.GetEnumerator();
        }

        public bool Remove([NotNullWhen(true)] ILogListener? item)
        {
            if (item is null || !this.backingCollection.Remove(item))
                return false;

            this.activeLogLevels = LogLevel.None;

            foreach (ILogListener listener in this)
            {
                this.activeLogLevels |= listener.LogLevelFilter;
            }

            return true;
        }
    }

    private sealed class LogSourceCollection :
        ICollection<ILogSource>,
        ICollection,
        IReadOnlyCollection<ILogSource>,
        IEnumerable<ILogSource>,
        IEnumerable
    {
        private readonly List<ILogSource> backingCollection;

        public LogSourceCollection()
        {
            this.backingCollection = new();
        }

        public int Count => this.backingCollection.Count;

        bool ICollection<ILogSource>.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        public void Add(ILogSource item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item), "Log sources cannot be null when added to the source list.");

            item.LogEvent += Logger.InternalLogEvent;

            this.backingCollection.Add(item);
        }

        public bool Contains([NotNullWhen(true)] ILogSource? item)
        {
            return this.backingCollection.Contains(item!);
        }

        public void CopyTo(ILogSource[] array, int arrayIndex)
        {
            this.backingCollection.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.backingCollection).CopyTo(array, index);
        }

        public void Clear()
        {
            foreach (ILogSource source in this)
            {
                source.LogEvent -= Logger.InternalLogEvent;
            }

            this.backingCollection.Clear();
        }

        public List<ILogSource>.Enumerator GetEnumerator()
        {
            return this.backingCollection.GetEnumerator();
        }

        IEnumerator<ILogSource> IEnumerable<ILogSource>.GetEnumerator()
        {
            return this.backingCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.backingCollection.GetEnumerator();
        }

        public bool Remove([NotNullWhen(true)] ILogSource? item)
        {
            if (item is null || !this.backingCollection.Remove(item))
                return false;

            item.LogEvent -= Logger.InternalLogEvent;

            return true;
        }
    }
}
