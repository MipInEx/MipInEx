using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// Information about a mod assembly.
/// </summary>
public sealed class ModAssemblyInfo : IModAssetInfo
{
    private ModInfo mod;
    private ModAssetState state;
    private readonly string name;
    private readonly string assetPath;
    private readonly string fullAssetPath;
    private readonly ModAssemblyRootPluginInfo rootPlugin;
    private readonly InternalPluginCollection internalPlugins;

    internal ModAssemblyInfo(
        string name,
        string assetPath,
        string fullAssetPath,
        ModAssemblyRootPluginInfo rootPlugin,
        ImmutableArray<ModAssemblyInternalPluginInfo> internalPlugins)
    {
        this.mod = null!;
        this.state = ModAssetState.NotLoaded;
        this.name = name;
        this.assetPath = assetPath;
        this.fullAssetPath = fullAssetPath;
        this.rootPlugin = rootPlugin;
        this.internalPlugins = new InternalPluginCollection(internalPlugins);
    }

    /// <inheritdoc cref="ModAssembly.RootPlugin"/>
    public ModAssemblyRootPluginInfo RootPlugin => this.rootPlugin;

    /// <inheritdoc cref="ModAssembly.InternalPlugins"/>
    public InternalPluginCollection InternalPlugins => this.internalPlugins;

    /// <inheritdoc cref="ModAssembly.Mod"/>
    public ModInfo Mod => this.mod;

    /// <inheritdoc cref="ModAssembly.Name"/>
    public string Name => this.name;

    /// <inheritdoc cref="ModAssembly.AssetPath"/>
    public string AssetPath => this.assetPath;

    /// <inheritdoc cref="ModAssembly.FullAssetPath"/>
    public string FullAssetPath => this.fullAssetPath;

    /// <inheritdoc cref="ModAssembly.State"/>
    public ModAssetState State => this.state;

    /// <inheritdoc cref="ModAssembly.IsLoaded"/>
    public bool IsLoaded => this.state == ModAssetState.Loaded;

    /// <inheritdoc cref="ModAssembly.IsUnloaded"/>
    public bool IsUnloaded => this.state is ModAssetState.Unloaded or ModAssetState.NotLoaded;

    /// <inheritdoc cref="ModAssembly.Type"/>
    public ModAssetType Type => ModAssetType.Assembly;

    /// <inheritdoc cref="ModAssembly.GetDescriptorString()"/>
    public string GetDescriptorString()
    {
        return $"Assembly '{this.name}'";
    }

    /// <inheritdoc cref="ModAssembly.ToString()"/>
    public sealed override string ToString()
    {
        return this.GetDescriptorString();
    }

    internal void Initialize(ModInfo mod)
    {
        this.mod = mod;
        this.rootPlugin.Initialize(this);
        this.internalPlugins.Initialize(this);
    }

    internal void SetState(ModAssetState state)
    {
        this.state = state;
    }

    /// <summary>
    /// A collection of
    /// <see cref="ModAssemblyInternalPluginInfo"/>s.
    /// </summary>
    public sealed class InternalPluginCollection :
        IReadOnlyList<ModAssemblyInternalPluginInfo>,
        IReadOnlyCollection<ModAssemblyInternalPluginInfo>,

        IList<ModAssemblyInternalPluginInfo>,
        ICollection<ModAssemblyInternalPluginInfo>,

        IList,
        ICollection,

        IEnumerable<ModAssemblyInternalPluginInfo>,
        IEnumerable,

        IEquatable<InternalPluginCollection>
    {
        private readonly ImmutableArray<ModAssemblyInternalPluginInfo> internalPlugins;
        private readonly FrozenDictionary<string, ModAssemblyInternalPluginInfo> guidToInternalPluginDictionary;

        internal InternalPluginCollection(ImmutableArray<ModAssemblyInternalPluginInfo> internalPlugins)
        {
            this.internalPlugins = internalPlugins;
            if (internalPlugins.IsEmpty)
            {
                this.guidToInternalPluginDictionary = FrozenDictionary<string, ModAssemblyInternalPluginInfo>.Empty;
            }
            else
            {
                this.guidToInternalPluginDictionary = this.internalPlugins.ToFrozenDictionary(
                    keySelector: (internalPlugin) => internalPlugin.Metadata.Guid);
            }
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.Count"/>
        public int Count => this.internalPlugins.Length;

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.Guids"/>
        public IReadOnlyList<string> Guids => this.guidToInternalPluginDictionary.Keys;

        bool ICollection<ModAssemblyInternalPluginInfo>.IsReadOnly => true;
        bool IList.IsReadOnly => true;
        bool IList.IsFixedSize => true;
        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => this;

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.this[int]"/>
        public ModAssemblyInternalPluginInfo this[int index]
            => this.internalPlugins[index];

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.this[string]"/>
        public ModAssemblyInternalPluginInfo this[string guid]
        {
            get
            {
                if (guid is null)
                    throw new ArgumentNullException(nameof(guid));

                return this.guidToInternalPluginDictionary[guid];
            }
        }

        ModAssemblyInternalPluginInfo IList<ModAssemblyInternalPluginInfo>.this[int index]
        {
            get => this.internalPlugins[index];
            set => throw new NotSupportedException();
        }

        object IList.this[int index]
        {
            get => this.internalPlugins[index];
            set => throw new NotSupportedException();
        }

        void ICollection<ModAssemblyInternalPluginInfo>.Add(ModAssemblyInternalPluginInfo item)
            => throw new NotSupportedException();

        int IList.Add(object value)
            => throw new NotSupportedException();

        void ICollection<ModAssemblyInternalPluginInfo>.Clear()
            => throw new NotSupportedException();

        void IList.Clear()
            => throw new NotSupportedException();

        bool ICollection<ModAssemblyInternalPluginInfo>.Contains(ModAssemblyInternalPluginInfo item)
            => this.ContainsInternalPlugin(item);

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.ContainsGuid(string?)"/>
        public bool ContainsGuid([NotNullWhen(true)] string? guid)
        {
            return guid is not null && this.guidToInternalPluginDictionary.ContainsKey(guid);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.ContainsInternalPlugin(ModAssemblyInternalPlugin?)"/>
        public bool ContainsInternalPlugin([NotNullWhen(true)] ModAssemblyInternalPluginInfo? internalPlugin)
        {
            return internalPlugin is not null && this.internalPlugins.Contains(internalPlugin);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.ContainsInternalPlugin(ModAssemblyInternalPlugin?, IEqualityComparer{ModAssemblyInternalPlugin}?)"/>
        public bool ContainsInternalPlugin([NotNullWhen(true)] ModAssemblyInternalPluginInfo? internalPlugin, IEqualityComparer<ModAssemblyInternalPluginInfo>? equalityComparer)
        {
            return internalPlugin is not null && this.internalPlugins.Contains(internalPlugin, equalityComparer);
        }

        bool IList.Contains(object? value)
        {
            return this.ContainsInternalPlugin(value as ModAssemblyInternalPluginInfo);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.CopyTo(Span{ModAssemblyInternalPlugin})"/>
        public void CopyTo(Span<ModAssemblyInternalPluginInfo> destination)
        {
            this.internalPlugins.CopyTo(destination);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.CopyTo(ModAssemblyInternalPlugin[])"/>
        public void CopyTo(ModAssemblyInternalPluginInfo[] destination)
        {
            this.internalPlugins.CopyTo(destination);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.CopyTo(ModAssemblyInternalPlugin[], int)"/>
        public void CopyTo(ModAssemblyInternalPluginInfo[] destination, int destinationIndex)
        {
            this.internalPlugins.CopyTo(destination, destinationIndex);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.CopyTo(int, ModAssemblyInternalPlugin[], int, int)"/>
        public void CopyTo(int sourceIndex, ModAssemblyInternalPluginInfo[] destination, int destinationIndex, int length)
        {
            this.internalPlugins.CopyTo(sourceIndex, destination, destinationIndex, length);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.internalPlugins).CopyTo(array, index);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.Equals(object?)"/>
        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null)
                return false;
            else if (obj is InternalPluginCollection internalPluginCollection)
                return this.internalPlugins.Equals(internalPluginCollection.internalPlugins);
            else
                return false;
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.Equals(ModAssembly.InternalPluginCollection?)"/>
        public bool Equals([NotNullWhen(true)] InternalPluginCollection? other)
        {
            if (other is null)
                return false;
            else
                return this.internalPlugins.Equals(other.internalPlugins);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.GetEnumerator()"/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<ModAssemblyInternalPluginInfo> IEnumerable<ModAssemblyInternalPluginInfo>.GetEnumerator()
        {
            return ((IEnumerable<ModAssemblyInternalPluginInfo>)this.internalPlugins).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.internalPlugins).GetEnumerator();
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.GetHashCode()"/>
        public sealed override int GetHashCode()
        {
            return this.internalPlugins.GetHashCode();
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.IndexOf(ModAssemblyInternalPlugin?)"/>
        public int IndexOf(ModAssemblyInternalPluginInfo? internalPlugin)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.IndexOf(ModAssemblyInternalPlugin?, int)"/>
        public int IndexOf(ModAssemblyInternalPluginInfo? internalPlugin, int startIndex)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, startIndex);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.IndexOf(ModAssemblyInternalPlugin?, int, int)"/>
        public int IndexOf(ModAssemblyInternalPluginInfo? internalPlugin, int startIndex, int count)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, startIndex, count);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.IndexOf(ModAssemblyInternalPlugin?, IEqualityComparer{ModAssemblyInternalPlugin}?)"/>
        public int IndexOf(ModAssemblyInternalPluginInfo? internalPlugin, IEqualityComparer<ModAssemblyInternalPluginInfo>? equalityComparer)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, 0, this.Count, equalityComparer);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.IndexOf(ModAssemblyInternalPlugin?, int, IEqualityComparer{ModAssemblyInternalPlugin}?)"/>
        public int IndexOf(ModAssemblyInternalPluginInfo? internalPlugin, int startIndex, IEqualityComparer<ModAssemblyInternalPluginInfo>? equalityComparer)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, startIndex, equalityComparer);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.IndexOf(ModAssemblyInternalPlugin?, int, int, IEqualityComparer{ModAssemblyInternalPlugin}?)"/>
        public int IndexOf(ModAssemblyInternalPluginInfo? internalPlugin, int startIndex, int count, IEqualityComparer<ModAssemblyInternalPluginInfo>? equalityComparer)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, startIndex, count, equalityComparer);
        }

        int IList.IndexOf(object? value)
        {
            if (value is not ModAssemblyInternalPluginInfo internalPlugin)
                return -1;
            else
                return this.internalPlugins.IndexOf(internalPlugin, 0, this.Count, EqualityComparer<ModAssemblyInternalPluginInfo>.Default);
        }

        internal void Initialize(ModAssemblyInfo assembly)
        {
            foreach (ModAssemblyInternalPluginInfo internalPlugin in this.internalPlugins)
            {
                internalPlugin.Initialize(assembly);
            }
        }

        void IList<ModAssemblyInternalPluginInfo>.Insert(int index, ModAssemblyInternalPluginInfo item)
            => throw new NotSupportedException();

        void IList.Insert(int index, object? value)
            => throw new NotSupportedException();

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.LastIndexOf(ModAssemblyInternalPlugin?)"/>
        public int LastIndexOf(ModAssemblyInternalPluginInfo? internalPlugin)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.LastIndexOf(ModAssemblyInternalPlugin?, int)"/>
        public int LastIndexOf(ModAssemblyInternalPluginInfo? internalPlugin, int startIndex)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, startIndex);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.LastIndexOf(ModAssemblyInternalPlugin?, int, int)"/>
        public int LastIndexOf(ModAssemblyInternalPluginInfo? internalPlugin, int startIndex, int count)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, startIndex, count);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.LastIndexOf(ModAssemblyInternalPlugin?, IEqualityComparer{ModAssemblyInternalPlugin}?)"/>
        public int LastIndexOf(ModAssemblyInternalPluginInfo? internalPlugin, IEqualityComparer<ModAssemblyInternalPluginInfo>? equalityComparer)
        {
            if (internalPlugin is null || this.internalPlugins.IsEmpty)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, this.Count - 1, this.Count, equalityComparer);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.LastIndexOf(ModAssemblyInternalPlugin?, int, IEqualityComparer{ModAssemblyInternalPlugin}?)"/>
        public int LastIndexOf(ModAssemblyInternalPluginInfo? internalPlugin, int startIndex, IEqualityComparer<ModAssemblyInternalPluginInfo>? equalityComparer)
        {
            if (internalPlugin is null || (this.internalPlugins.IsEmpty && startIndex == 0))
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, startIndex, startIndex + 1, equalityComparer);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.LastIndexOf(ModAssemblyInternalPlugin?, int, int, IEqualityComparer{ModAssemblyInternalPlugin}?)"/>
        public int LastIndexOf(ModAssemblyInternalPluginInfo? internalPlugin, int startIndex, int count, IEqualityComparer<ModAssemblyInternalPluginInfo>? equalityComparer)
        {
            if (internalPlugin is null)
                return -1;
            else
                return this.internalPlugins.LastIndexOf(internalPlugin, startIndex, count, equalityComparer);
        }

        bool ICollection<ModAssemblyInternalPluginInfo>.Remove(ModAssemblyInternalPluginInfo item)
            => throw new NotSupportedException();
        void IList.Remove(object? value)
            => throw new NotSupportedException();

        void IList<ModAssemblyInternalPluginInfo>.RemoveAt(int index)
            => throw new NotSupportedException();
        void IList.RemoveAt(int index)
            => throw new NotSupportedException();

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.TryGetInternalPlugin(string?, out ModAssemblyInternalPlugin?)"/>
        public bool TryGetInternalPlugin([NotNullWhen(true)] string? guid, [NotNullWhen(true)] out ModAssemblyInternalPluginInfo? internalPlugin)
        {
            if (guid is null)
            {
                internalPlugin = null;
                return false;
            }

            return this.guidToInternalPluginDictionary.TryGetValue(guid, out internalPlugin);
        }

        /// <inheritdoc cref="ModAssembly.InternalPluginCollection.Enumerator"/>
        public readonly struct Enumerator
        {
            private readonly ImmutableArray<ModAssemblyInternalPluginInfo>.Enumerator enumerator;

            internal Enumerator(InternalPluginCollection collection)
            {
                this.enumerator = collection.internalPlugins.GetEnumerator();
            }

            /// <inheritdoc cref="ModAssembly.InternalPluginCollection.Enumerator.Current"/>
            public ModAssemblyInternalPluginInfo Current => this.enumerator.Current;

            /// <inheritdoc cref="ModAssembly.InternalPluginCollection.Enumerator.MoveNext"/>
            public bool MoveNext()
                => this.enumerator.MoveNext();
        }
    }
}
