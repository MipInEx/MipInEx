using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MipInEx;

/// <summary>
/// A registry of all mods.
/// </summary>
public sealed class ModRegistry
{
    private readonly FrozenDictionary<string, ModInfo> mods;

    internal ModRegistry(IReadOnlyCollection<KeyValuePair<string, Mod>> mods)
    {
        if (mods.Count == 0)
        {
            this.mods = FrozenDictionary<string, ModInfo>.Empty;
            return;
        }

        this.mods = mods.ToFrozenDictionary(
            entry => entry.Key,
            entry => entry.Value.Info);
    }

    /// <summary>
    /// The GUIDs of the mods in this registry.
    /// </summary>
    public IReadOnlyList<string> Guids
    {
        get => this.mods.Keys;
    }

    /// <summary>
    /// The mods in this registry.
    /// </summary>
    public IReadOnlyList<ModInfo> Mods
    {
        get => this.mods.Values;
    }

    /// <summary>
    /// Gets the number of mods in this registry.
    /// </summary>
    public int Count => this.mods.Count;

    /// <summary>
    /// Gets the mod with the specified GUID.
    /// </summary>
    /// <param name="guid">The GUID of the mod to fetch</param>
    /// <returns>
    /// The mod with the specified GUID.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="guid"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No mod with <paramref name="guid"/> exists.
    /// </exception>
    public ModInfo this[string guid]
    {
        get => this.mods[guid];
    }

    /// <summary>
    /// Determines whether or not this registry contains a mod
    /// with the specified <paramref name="guid"/>.
    /// </summary>
    /// <param name="guid">
    /// The GUID of the mod to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a mod with
    /// <paramref name="guid"/> exists; <see langword="false"/>
    /// otherwise.
    /// </returns>
    public bool ContainsMod([NotNullWhen(true)] string? guid)
    {
        return guid is not null && this.mods.ContainsKey(guid);
    }

    /// <summary>
    /// Determines whether or not this registry contains a mod
    /// with the specified <paramref name="guid"/> that is
    /// loaded.
    /// </summary>
    /// <param name="guid">
    /// The GUID of the mod to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a mod with
    /// <paramref name="guid"/> exists and is loaded;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsModLoaded([NotNullWhen(true)] string? guid)
    {
        return guid is not null &&
            this.mods.TryGetValue(guid, out ModInfo? mod) &&
            mod.IsLoaded;
    }

    /// <summary>
    /// Attempts to get the mod with the specified
    /// <paramref name="guid"/>.
    /// </summary>
    /// <param name="guid">
    /// The guid of the mod to get. Won't be null if this
    /// method returns <see langword="true"/>.
    /// </param>
    /// <param name="mod">
    /// If this method returns <see langword="true"/>, then
    /// the value will be the found mod, otherwise if this
    /// method returns <see langword="false"/>, then the value
    /// will be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this registry contains a mod
    /// with the specified <paramref name="guid"/>;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public bool TryGetMod([NotNullWhen(true)] string? guid, [NotNullWhen(true)] out ModInfo? mod)
    {
        if (guid is null)
        {
            mod = null;
            return false;
        }
        else
        {
            return this.mods.TryGetValue(guid, out mod);
        }
    }
}
